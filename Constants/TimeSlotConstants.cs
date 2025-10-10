/*
 * TimeSlotConstants.cs
 * 
 * This file contains time slot-related constants and utility methods.
 * It defines station operating hours, slot durations, predefined time slots,
 * and provides helper methods for time slot validation and management.
 */

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

        // Validates if the given DateTime represents a valid predefined time slot
        public static bool IsValidTimeSlot(DateTime dateTime)
        {
            var timeOfDay = dateTime.TimeOfDay;
            return PredefinedTimeSlots.Contains(timeOfDay);
        }
       
        // Calculates the end time for a given slot start time
        public static DateTime GetSlotEndTime(DateTime slotStartTime)
        {
            var endTime = slotStartTime.AddHours(SlotDurationHours);
            // Ensure the result maintains the same DateTimeKind as the input
            return slotStartTime.Kind == DateTimeKind.Utc ? DateTime.SpecifyKind(endTime, DateTimeKind.Utc) : endTime;
        }

        // Gets the display name for a given slot start time
        public static string GetSlotDisplayName(DateTime slotStartTime)
        {
            var timeOfDay = slotStartTime.TimeOfDay;
            return SlotDisplayNames.TryGetValue(timeOfDay, out var displayName) 
                ? displayName 
                : $"{slotStartTime:HH:mm} - {GetSlotEndTime(slotStartTime):HH:mm}";
        }

        // Generates all available time slots for a specific date
        public static List<DateTime> GetAvailableTimeSlotsForDate(DateTime date)
        {
            // Ensure we work with UTC dates to match booking storage format
            var dateOnly = date.Kind == DateTimeKind.Utc ? date.Date : DateTime.SpecifyKind(date.Date, DateTimeKind.Utc);
            return PredefinedTimeSlots.Select(timeSlot => DateTime.SpecifyKind(dateOnly.Add(timeSlot), DateTimeKind.Utc)).ToList();
        }

        // Checks if a slot start time is within station operating hours
        public static bool IsWithinOperatingHours(DateTime slotStartTime)
        {
            var timeOfDay = slotStartTime.TimeOfDay;
            var slotEndTime = slotStartTime.AddHours(SlotDurationHours).TimeOfDay;

            if (slotEndTime == TimeSpan.Zero)
                slotEndTime = new TimeSpan(24, 0, 0);
                
            return timeOfDay >= StationOpenTime && slotEndTime <= StationCloseTime;
        }
    }
}