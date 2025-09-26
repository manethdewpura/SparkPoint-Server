# Booking Slot Management System

## Overview
The BookingsController has been enhanced with intelligent slot management that automatically updates available slots in charging stations based on booking status changes.

## Key Features Implemented

### 1. Automatic Slot Management
- **Status-Based Slot Allocation**: Slots are automatically reserved/freed based on booking status
- **Real-Time Availability**: Available slots are updated in real-time when status changes occur
- **Data Consistency**: Ensures slot counts remain accurate across all operations

### 2. Status-Based Slot Logic

#### Slot Reserving Statuses (Occupy a slot):
- `Confirmed` - Booking is confirmed, slot is reserved
- `In Progress` - EV is currently charging, slot is occupied

#### Slot Freeing Statuses (Free a slot):
- `Completed` - Charging session finished, slot becomes available
- `Cancelled` - Booking cancelled, slot becomes available
- `No Show` - Customer didn't show up, slot becomes available

#### Neutral Status:
- `Pending` - No slot impact until confirmed

### 3. New Components

#### BookingService (`Services/BookingService.cs`)
- **UpdateBookingStatus()**: Updates status and manages slots automatically
- **CheckSlotAvailability()**: Verifies if slots are available at specific times
- **GetAvailableSlotsAtTime()**: Returns available slot count for a specific time
- **RecalculateStationAvailableSlots()**: Utility for data consistency maintenance

#### BookingStatusConstants (`Utils/BookingStatusConstants.cs`)
- Centralized status constants and validation methods
- Helper methods for status categorization
- Ensures consistent status handling across the application

### 4. Enhanced Controller Features

#### Updated Endpoints:
- **POST /api/bookings**: Now returns available slot information
- **PUT /api/bookings/status/{id}**: Automatically manages slots on status change
- **PUT /api/bookings/cancel/{id}**: Properly frees slots when canceling
- **GET /api/bookings/availability/{stationId}**: New endpoint for checking availability

#### Slot Management Logic:
```csharp
// Status transitions and their slot impact:
Pending ? Confirmed: Reserve 1 slot (-1)
Confirmed ? Completed: Free 1 slot (+1)
Confirmed ? Cancelled: Free 1 slot (+1)
In Progress ? Completed: Free 1 slot (+1)
In Progress ? No Show: Free 1 slot (+1)
```

### 5. Usage Examples

#### Creating a Booking:
```json
POST /api/bookings
{
    "StationId": "station123",
    "ReservationTime": "2024-01-15T10:00:00Z",
    "OwnerNIC": "123456789V"
}

Response:
{
    "Message": "Booking created successfully.",
    "BookingId": "booking456",
    "AvailableSlotsAtTime": 3
}
```

#### Updating Status to Confirmed (Reserves Slot):
```json
PUT /api/bookings/status/booking456
{
    "Status": "Confirmed"
}

Response: "Booking status updated to Confirmed. Reserved 1 slot(s)."
```

#### Completing a Booking (Frees Slot):
```json
PUT /api/bookings/status/booking456
{
    "Status": "Completed"
}

Response: "Booking status updated to Completed. Freed 1 slot(s)."
```

#### Checking Availability:
```json
GET /api/bookings/availability/station123?reservationTime=2024-01-15T10:00:00Z

Response:
{
    "StationId": "station123",
    "ReservationTime": "2024-01-15T10:00:00Z",
    "TotalSlots": 5,
    "AvailableSlots": 3,
    "IsAvailable": true
}
```

### 6. Business Rules Enforced

#### Slot Availability Validation:
- Checks actual bookings at specific time slots
- Prevents overbooking by validating availability before confirmation
- Excludes cancelled/completed bookings from availability calculations

#### Status Transition Rules:
- Maintains data integrity during status changes
- Prevents invalid status transitions
- Ensures slots are properly managed during updates

#### Time-Based Restrictions:
- Different status options available based on time to reservation
- Automatic slot management respects business timing rules

### 7. Error Handling

The system includes comprehensive error handling for:
- Invalid station IDs
- Slot unavailability scenarios
- Invalid status transitions
- Data consistency issues

### 8. Data Consistency Features

#### Recalculation Utility:
The `RecalculateStationAvailableSlots()` method can be used for:
- Database maintenance
- Fixing any slot inconsistencies
- Periodic data validation

#### Automatic Validation:
- Real-time availability checking
- Slot count boundaries (0 ? available ? total)
- Conflict detection for time slots

## Benefits

1. **Real-Time Accuracy**: Available slots always reflect current booking statuses
2. **Automatic Management**: No manual intervention needed for slot updates  
3. **Business Logic Compliance**: Follows EV charging station booking rules
4. **Data Integrity**: Maintains consistent state across all operations
5. **User Experience**: Provides immediate feedback on slot availability
6. **Scalability**: Handles multiple concurrent bookings efficiently

This implementation ensures that the charging station slot management is fully automated, accurate, and follows proper business logic for EV charging station operations.