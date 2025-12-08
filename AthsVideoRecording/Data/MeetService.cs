using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AthsVideoRecording.Data
{

    public class MeetService
    {
        public void AddMeet(Meet meet)
        {
            using var context = new AthStitcherDbContext();
            context.Meets.Add(meet);
            context.SaveChanges();
        }

        public void UpdateMeet(Meet meet)
        {
            using var context = new AthStitcherDbContext();
            context.Meets.Update(meet);
            context.SaveChanges();
        }

        public void DeleteMeet(Meet meet)
        {
            using var context = new AthStitcherDbContext();
            context.Meets.Remove(meet);
            context.SaveChanges();
        }

        public List<Meet> GetAllMeets()
        {
            using var context = new AthStitcherDbContext();
            return context.Meets.OrderByDescending(m => m.Date).ToList();
        }
    }

}
