# SparkPoint Server

A comprehensive ASP.NET Web API backend for managing electric vehicle charging stations and bookings.

## Overview

SparkPoint Server is a RESTful API built with ASP.NET Web API 2 and MongoDB that provides complete functionality for:

- **User Management**: Registration, authentication, and role-based access control
- **Charging Station Management**: CRUD operations for EV charging stations
- **Booking System**: Time-slot based charging station reservations
- **Authentication**: JWT-based authentication with refresh token rotation
- **Authorization**: Role-based access control (Admin, EV Owner, Station User)

## Technology Stack

- **Framework**: ASP.NET Web API 2 (.NET Framework 4.8)
- **Database**: MongoDB
- **Authentication**: JWT with refresh token rotation
- **Security**: BCrypt password hashing, CORS support
- **Architecture**: Clean architecture with Controllers, Services, Models, and Helpers

## Features

### Authentication & Authorization
- JWT-based authentication with access and refresh tokens
- Support for both web clients (cookies) and mobile clients (tokens)
- Role-based authorization (Admin, EV Owner, Station User)
- Session management with active session tracking
- Secure password hashing with BCrypt

### User Management
- User registration and profile management
- Role assignment and management
- User status tracking (active/inactive)

### Charging Station Management
- Create, read, update, and delete charging stations
- Station activation/deactivation
- Station statistics and analytics
- Slot management for charging stations

### Booking System
- Time-slot based booking system
- Booking status management
- Booking validation and conflict resolution
- Booking history and analytics

## Project Structure

```
├── Controllers/          # API controllers
├── Services/            # Business logic services
├── Models/              # Data models and DTOs
├── Helpers/             # Utility classes and helpers
├── Utils/               # Utility functions
├── Constants/           # Application constants
├── Enums/               # Enumeration definitions
├── Middleware/          # Custom middleware
└── Attributes/          # Custom attributes
```

## Getting Started

### Prerequisites

- .NET Framework 4.8
- MongoDB server
- Visual Studio 2019 or later (for development)

### Installation

1. Clone the repository
2. Restore NuGet packages
3. Configure MongoDB connection in `Web.config`
4. Build and run the project

### Configuration

Update the following settings in `Web.config`:

```xml
<appSettings>
    <add key="MongoDbConnection" value="your-mongodb-connection-string" />
    <add key="MongoDbDatabase" value="SparkPointDev" />
    <add key="Environment" value="Development" />
    <add key="CORS:AllowedOrigins" value="*" />
</appSettings>
```

## API Endpoints

### Authentication
- `POST /api/auth/login` - User login
- `POST /api/auth/refresh` - Refresh access token
- `POST /api/auth/logout` - User logout
- `GET /api/auth/sessions/{userId}` - Get active sessions
- `DELETE /api/auth/sessions/{userId}/{tokenId}` - Revoke session

### Users
- `GET /api/users` - Get users (with filtering)
- `GET /api/users/{userId}` - Get user by ID
- `POST /api/users` - Create user (admin only)
- `PATCH /api/users/{userId}` - Update user
- `DELETE /api/users/{userId}` - Delete user (admin only)

### Charging Stations
- `POST /api/stations` - Create station (admin only)
- `GET /api/stations` - Get stations (with filtering)
- `GET /api/stations/{stationId}` - Get station by ID
- `PATCH /api/stations/{stationId}` - Update station (admin only)
- `PATCH /api/stations/activate/{stationId}` - Activate station (admin only)
- `PATCH /api/stations/deactivate/{stationId}` - Deactivate station (admin only)
- `PATCH /api/stations/{stationId}/slots` - Update station slots (station user only)

### Bookings
- `POST /api/bookings` - Create booking
- `GET /api/bookings` - Get bookings (with filtering)
- `GET /api/bookings/{bookingId}` - Get booking by ID
- `PATCH /api/bookings/{bookingId}` - Update booking
- `DELETE /api/bookings/{bookingId}` - Cancel booking

## Security Features

- **JWT Authentication**: Secure token-based authentication
- **Refresh Token Rotation**: Enhanced security with token rotation
- **Role-Based Authorization**: Granular access control
- **Password Security**: BCrypt hashing for password storage
- **CORS Support**: Configurable cross-origin resource sharing
- **Rate Limiting**: Built-in rate limiting middleware
- **Input Validation**: Comprehensive request validation

## Development

### Building the Project

```bash
# Restore packages
nuget restore

# Build solution
msbuild SparkPoint-Server.sln
```

