using AthsVideoRecording.Data;
using AthsVideoRecording.Views;
using MauiAndroidVideoCaptureApp;
using MauiCountdownToolkit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using SendVideoOverTCPLib.ViewModels;
using Sportronics.VideoEnums;
using System;
using System;
// Ensure that the necessary namespaces are included at the top of the file.  
using System;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks;

namespace AthsVideoRecording;


public partial class MainPage : ContentPage, IDisposable
{
    private async void MainPageLoaded()
    {

        try
        {

            await Task.Run(() =>
            {

                using var ctx = new AthsVideoRecording.Data.AthsVideoRecordingDbContext();
                if (NewDatabase)
                {
                    ctx.Database.EnsureDeleted();
                    NewDatabase = false;
                    SendVideoOverTCPLib.Settings.SetNewDatabaseSetting(NewDatabase);

                }
                ctx.Database.EnsureCreated();
                //ctx.Database.Migrate();
            });
            // Seeding uses short operations; OK on UI thread post-migrate
            await SeedAdminIfMissing();
            await EnforceForcePasswordChangeIfNeeded();
            BusyIndicatorInd.IsRunning = false;
            BusyIndicatorInd.IsVisible = false;
            BusyIndicator.IsVisible = false;
            BusyIndicatorLabel.IsVisible = false;
            MyLayout1.IsVisible = true;
            MyLayout2.IsVisible = true;
            MyLayout3.IsVisible = true;
            await DisplayAlert("Database", $"All good!", "OK");

        }
        catch (System.Exception ex2)
        {
            BusyIndicatorInd.IsRunning = false;
            BusyIndicatorInd.IsVisible = false;
            BusyIndicator.IsVisible = false;
            BusyIndicatorLabel.IsVisible = false;
            MyLayout1.IsVisible = true;
            MyLayout2.IsVisible = true;
            MyLayout3.IsVisible = true;
            await DisplayAlert("Database", $"Database initialization error: {ex2.Message}", "OK");
        }

    }
    private async Task DeleteAndRecreateDatabase_Menu_Click(object sender, EventArgs e)
    {
        var confirm = await DisplayAlert(
            "Delete Database",
            "This will delete the local AthStitcher database file and recreate it. Continue?",
            "Yes",
            "No"
            );
        if (!confirm)
            return;

        try
        {
            using var ctx = new AthsVideoRecording.Data.AthsVideoRecordingDbContext();
            if (!ctx.Database.EnsureDeleted())
            {
                // If ensure delete returns false or fails due to locks, try to drop all tables then continue
                try { ctx.Database.ExecuteSqlRaw("PRAGMA foreign_keys = OFF;"); }
                catch { }
                try
                {
                    ctx.Database.ExecuteSqlRaw("DROP TABLE IF EXISTS Meets;");
                    ctx.Database.ExecuteSqlRaw("DROP TABLE IF EXISTS Events;");
                    ctx.Database.ExecuteSqlRaw("DROP TABLE IF EXISTS Users;");
                }
                catch { }
                try { ctx.Database.ExecuteSqlRaw("PRAGMA foreign_keys = ON;"); }
                catch { }
            }
            ctx.Database.Migrate();
            await SeedAdminIfMissing();
            await DisplayAlert("Database", "Database recreated successfully.", "OK");
        }
        catch (System.Exception ex)
        {
            await DisplayAlert("Database Error", $"Failed to delete/recreate database: {ex.Message}", "OK");
        }
    }

    private async Task SeedAdminIfMissing()
    {
        using var ctx = new AthsVideoRecording.Data.AthsVideoRecordingDbContext();
        var admin = ctx.Users.SingleOrDefault(u => u.Username == "admin");
        if (admin == null)
        {
            string tempPwd = AthsVideoRecording.Security.PasswordHasher.GenerateRandomPassword(24);
            var (hash, salt) = AthsVideoRecording.Security.PasswordHasher.HashPassword(tempPwd);
            admin = new AthsVideoRecording.Data.User
            {
                Username = "admin",
                PasswordHash = hash,
                PasswordSalt = salt,
                Role = "Admin",
                CreatedAt = DateTime.UtcNow,
                ForcePasswordChange = true
            };
            ctx.Users.Add(admin);
            ctx.SaveChanges();
            try { await Clipboard.SetTextAsync(tempPwd); } catch { }
            await DisplayAlert("Admin Created", $"Admin user created.\n\nUsername: admin\nTemporary Password (copied to clipboard):\n{tempPwd}\n\nYou will be asked to change it on first login.",
                 "OK");
        }
    }

    private async Task EnforceForcePasswordChangeIfNeeded()
    {
        using var ctx = new AthsVideoRecording.Data.AthsVideoRecordingDbContext();
        var admin = ctx.Users.SingleOrDefault(u => u.Username == "admin");
        if (admin != null && admin.ForcePasswordChange)
        {
            if (this.IsLoaded)
            {
                await ChangePasswordForUser("admin", requireCurrent: false);
            }
            else
            {
                this.Loaded += async (_, __) => await ChangePasswordForUser("admin", requireCurrent: false);
            }
        }
    }

    private async Task ChangePasswordForUser(string username, bool requireCurrent)
    {
        //var dlg = new AthsVideoRecording.Views.ChangePasswordDialog { Username = username };
        //if (this.IsLoaded && this.IsVisible) dlg.Owner = this;
        //if (this.ShowPopup< ChangePasswordDialog>(dlg) != true) return;


        var modal = new ChangePasswordDialog();
        await Navigation.PushModalAsync(modal);
        bool result = await modal.WaitForCloseAsync();

        using var ctx = new AthsVideoRecording.Data.AthsVideoRecordingDbContext();
        var user = ctx.Users.SingleOrDefault(u => u.Username == username);
        if (user == null)
        {
            await DisplayAlert("Change Password", $"User '{username}' not found.", "OK");
            return;
        }

        if (requireCurrent)
        {
            string current = modal.CurrentPassword;
            if (!AthsVideoRecording.Security.PasswordHasher.Verify(current, user.PasswordHash, user.PasswordSalt))
            {
                await DisplayAlert("Change Password", "Current password is incorrect.", "OK");
                return;
            }
        }

        var (hash, salt) = AthsVideoRecording.Security.PasswordHasher.HashPassword(modal.Password);
        user.PasswordHash = hash;
        user.PasswordSalt = salt;
        user.ForcePasswordChange = false;
        ctx.SaveChanges();
        await DisplayAlert("Change Password", "Password changed successfully.", "OK");

    }


    // Menu handler to invoke change password (wire from XAML MenuItem)
    private async Task ChangePassword_Menu_Click(object sender, EventArgs e)
    {
        await ChangePasswordForUser("admin", requireCurrent: true);
    }

    // Menu handler to reset admin password (one-time recovery)
    private async Task ResetAdminPassword_Menu_Click(object sender, EventArgs e)
    {
        try
        {
            using var conn = Db.CreateConnection();
            var repo = new UserRepository();
            var user = repo.GetByUsername(conn, "admin");
            if (user == null)
            {
                await DisplayAlert("Reset Password", "Admin user not found.", "OK");
                return;
            }

            string tempPwd = AthsVideoRecording.Security.PasswordHasher.GenerateRandomPassword(24);
            if (repo.ResetPassword(conn, user.Id, tempPwd, forceChange: true))
            {
                try { await Clipboard.SetTextAsync(tempPwd); } catch { }
                await DisplayAlert("Reset Password", $"Admin password reset.\n\nTemporary Password (copied to clipboard):\n{tempPwd}\n\nYou'll be asked to change it on next login.", "OK");
            }
            else
            {
                await DisplayAlert("Reset Password", "Failed to reset admin password.", "OK");
            }
        }
        catch (System.Exception ex)
        {
            await DisplayAlert("Reset Password", $"Error resetting password: {ex.Message}", "OK");
        }
    }
}
