using System;
using System.Collections.Generic;
using MongoDB.Driver;
using SparkPoint_Server.Models;
using SparkPoint_Server.Constants;
using SparkPoint_Server.Enums;

namespace SparkPoint_Server.Utils
{

    public static class BookingFilterUtils
    {

        public static FilterDefinition<Booking> BuildFilter(BookingFilterModel filter, FilterDefinition<Booking> baseFilter = null)
        {
            var filterBuilder = baseFilter ?? Builders<Booking>.Filter.Empty;

            if (filter == null)
                return filterBuilder;

            // Status filter
            if (!string.IsNullOrEmpty(filter.Status))
            {
                var statusFilter = Builders<Booking>.Filter.Eq(b => b.Status, filter.Status);
                filterBuilder = Builders<Booking>.Filter.And(filterBuilder, statusFilter);
            }

            // Station filter
            if (!string.IsNullOrEmpty(filter.StationId))
            {
                var stationFilter = Builders<Booking>.Filter.Eq(b => b.StationId, filter.StationId);
                filterBuilder = Builders<Booking>.Filter.And(filterBuilder, stationFilter);
            }

            // Date range filters
            if (filter.FromDate.HasValue)
            {
                var fromDateFilter = Builders<Booking>.Filter.Gte(b => b.ReservationTime, filter.FromDate.Value);
                filterBuilder = Builders<Booking>.Filter.And(filterBuilder, fromDateFilter);
            }

            if (filter.ToDate.HasValue)
            {
                var toDateFilter = Builders<Booking>.Filter.Lte(b => b.ReservationTime, filter.ToDate.Value);
                filterBuilder = Builders<Booking>.Filter.And(filterBuilder, toDateFilter);
            }

            return filterBuilder;
        }

        public static FilterDefinition<Booking> GetActiveBookingsFilter()
        {
            return Builders<Booking>.Filter.Not(
                Builders<Booking>.Filter.In(b => b.Status, BookingStatusConstants.SlotFreeingStatuses)
            );
        }

        public static FilterDefinition<Booking> GetConflictingBookingsFilter(
            string stationId, 
            DateTime reservationTime, 
            string excludeBookingId = null)
        {
            var filterBuilder = Builders<Booking>.Filter.And(
                Builders<Booking>.Filter.Eq(b => b.StationId, stationId),
                Builders<Booking>.Filter.Eq(b => b.ReservationTime, reservationTime),
                GetActiveBookingsFilter()
            );

            if (!string.IsNullOrEmpty(excludeBookingId))
            {
                filterBuilder = Builders<Booking>.Filter.And(
                    filterBuilder,
                    Builders<Booking>.Filter.Ne(b => b.Id, excludeBookingId)
                );
            }

            return filterBuilder;
        }

        public static FilterDefinition<Booking> GetOwnerBookingsFilter(string ownerNIC)
        {
            return Builders<Booking>.Filter.Eq(b => b.OwnerNIC, ownerNIC);
        }

        public static FilterDefinition<Booking> GetStationBookingsFilter(string stationId)
        {
            return Builders<Booking>.Filter.Eq(b => b.StationId, stationId);
        }

        public static FilterDefinition<Booking> GetStatusFilter(string status)
        {
            return Builders<Booking>.Filter.Eq(b => b.Status, status);
        }

        public static FilterDefinition<Booking> GetMultipleStatusFilter(string[] statuses)
        {
            return Builders<Booking>.Filter.In(b => b.Status, statuses);
        }

        public static FilterDefinition<Booking> GetDateRangeFilter(DateTime? fromDate, DateTime? toDate)
        {
            var filters = new List<FilterDefinition<Booking>>();

            if (fromDate.HasValue)
            {
                filters.Add(Builders<Booking>.Filter.Gte(b => b.ReservationTime, fromDate.Value));
            }

            if (toDate.HasValue)
            {
                filters.Add(Builders<Booking>.Filter.Lte(b => b.ReservationTime, toDate.Value));
            }

            if (filters.Count == 0)
                return Builders<Booking>.Filter.Empty;

            if (filters.Count == 1)
                return filters[0];

            return Builders<Booking>.Filter.And(filters);
        }

        public static FilterDefinition<Booking> GetModifiableBookingsFilter(double hoursBuffer = ApplicationConstants.MinModificationHours)
        {
            var cutoffTime = DateTime.Now.AddHours(hoursBuffer);
            return Builders<Booking>.Filter.Gte(b => b.ReservationTime, cutoffTime);
        }

        public static SortDefinition<Booking> GetSortDefinition(BookingSortField sortBy = BookingSortField.CreatedAt, bool ascending = false)
        {
            switch (sortBy)
            {
                case BookingSortField.ReservationTime:
                    return ascending ? 
                        Builders<Booking>.Sort.Ascending(b => b.ReservationTime) : 
                        Builders<Booking>.Sort.Descending(b => b.ReservationTime);
                case BookingSortField.Status:
                    return ascending ? 
                        Builders<Booking>.Sort.Ascending(b => b.Status) : 
                        Builders<Booking>.Sort.Descending(b => b.Status);
                case BookingSortField.UpdatedAt:
                    return ascending ? 
                        Builders<Booking>.Sort.Ascending(b => b.UpdatedAt) : 
                        Builders<Booking>.Sort.Descending(b => b.UpdatedAt);
                case BookingSortField.CreatedAt:
                default:
                    return ascending ? 
                        Builders<Booking>.Sort.Ascending(b => b.CreatedAt) : 
                        Builders<Booking>.Sort.Descending(b => b.CreatedAt);
            }
        }
    }
}