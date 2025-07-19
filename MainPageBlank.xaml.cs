using Microsoft.Maui.Controls;
using System;
using System.Diagnostics;

namespace MauiCameraViewSample
{
    public partial class MainPageBlank : ContentPage
    {
        public MainPageBlank()
        {
            InitializeComponent();
            Debug.WriteLine("MainPage constructor executed");
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            Debug.WriteLine("MainPage OnAppearing executed");
        }

        private void OnNextPageClicked(object sender, EventArgs e)
        {
            try
            {
                Debug.WriteLine("Attempting to navigate to MainPage2");
                
                // Create an instance of MainPage2
                var mainPage = new MainPage();
                
                // Set it as the current page
                Application.Current.MainPage = mainPage;
                
                Debug.WriteLine("Successfully navigated to MainPage2");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Navigation error: {ex.Message}");
                DisplayAlert("Navigation Error", $"Could not navigate to MainPage2: {ex.Message}", "OK");
            }
        }
    }
}