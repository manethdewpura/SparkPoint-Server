using System;
using System.Collections.Generic;
using System.Linq;

namespace SparkPoint_Server.Constants
{
    public static class TimeSlotConstants
    {
        // Station operating hours
        public static readonly TimeSpan StationOpenTime = new TimeSpan(6, 0, 0);  // 6:00 AM
        public static readonly TimeSpan StationCloseTime = new TimeSpan(24, 0, 0); // 12:00 AM (midnight)
        
        // Slot duration in hours
        public const int SlotDurationHours = 2;
        
        // Predefined time slots (start times)
        public static readonly List<TimeSpan> PredefinedTimeSlots = new List<TimeSpan>
        {
            new TimeSpan(6, 0, 0),   // 6:00 AM - 8:00 AM
            new TimeSpan(8, 0, 0),   // 8:00 AM - 10:00 AM
            new TimeSpan(10, 0, 0),  // 10:00 AM - 12:00 PM
            new TimeSpan(12, 0, 0),  // 12:00 PM - 2:00 PM
            new TimeSpan(14, 0, 0),  // 2:00 PM - 4:00 PM
            new TimeSpan(16, 0, 0),  // 4:00 PM - 6:00 PM
            new TimeSpan(18, 0, 0),  // 6:00 PM - 8:00 PM
            new TimeSpan(20, 0, 0),  // 8:00 PM - 10:00 PM
            new TimeSpan(22, 0, 0)   // 10:00 PM - 12:00 AM
        };
        
        // Slot display names
        public static readonly Dictionary<TimeSpan, string> SlotDisplayNames = new Dictionary<TimeSpan, string>
        {
            { new TimeSpan(6, 0, 0), "6:00 AM - 8:00 AM" },
            { new TimeSpan(8, 0, 0), "8:00 AM - 10:00 AM" },
            { new TimeSpan(10, 0, 0), "10:00 AM - 12:00 PM" },
            { new TimeSpan(12, 0, 0), "12:00 PM - 2:00 PM" },
            { new TimeSpan(14, 0, 0), "2:00 PM - 4:00 PM" },
            { new TimeSpan(16, 0, 0), "4:00 PM - 6:00 PM" },
            { new TimeSpan(18, 0, 0), "6:00 PM - 8:00 PM" },
            { new TimeSpan(20, 0, 0), "8:00 PM - 10:00 PM" },
            { new TimeSpan(22, 0, 0), "10:00 PM - 12:00 AM" }
        };

        public static bool IsValidTimeSlot(DateTime dateTime)
        {
            var timeOfDay = dateTime.TimeOfDay;
            return PredefinedTimeSlots.Contains(timeOfDay);
        }
       
        public static DateTime GetSlotEndTime(DateTime slotStartTime)
        {
            return slotStartTime.AddHours(SlotDurationHours);
        }

        public static string GetSlotDisplayName(DateTime slotStartTime)
        {
            var timeOfDay = slotStartTime.TimeOfDay;
            return SlotDisplayNames.TryGetValue(timeOfDay, out var displayName) 
                ? displayName 
                : $"{slotStartTime:HH:mm} - {GetSlotEndTime(slotStartTime):HH:mm}";
        }

        public static List<DateTime> GetAvailableTimeSlotsForDate(DateTime date)
        {
            var dateOnly = date.Date;
            return PredefinedTimeSlots.Select(timeSlot => dateOnly.Add(timeSlot)).ToList();
        }

        public static bool IsWithinOperatingHours(DateTime slotStartTime)
        {
            var timeOfDay = slotStartTime.TimeOfDay;
            var slotEndTime = slotStartTime.AddHours(SlotDurationHours).TimeOfDay;
            S
            if (slotEndTime == TimeSpan.Zero)
                slotEndTime = new TimeSpan(24, 0, 0);
                
            return timeOfDay >= StationOpenTime && slotEndTime <= StationCloseTime;
        }
    }
}