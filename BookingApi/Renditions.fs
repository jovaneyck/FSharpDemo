module BookingApi.Renditions

open System

[<CLIMutable>]
type MakeReservationRendition = { 
        Name : string
        Date : string
        Email : string
        Quantity : int 
    }

[<CLIMutable>]
type NotificationRendition = {
    About : string
    Type : string
    Message : string
}

[<CLIMutable>]
type NotificationListRendition = {
    Notifications : NotificationRendition array
}

[<CLIMutable>]
type LinkRendition = {
    Rel : string
    Href : string
}

[<CLIMutable>]
type LinkListRendition = {
    Links : LinkRendition array
}

[<CLIMutable>]
type OpeningsRendition = {
    Date : string
    FreeSeats : int
}

[<CLIMutable>]
type AvailabilityRendition = {
    Openings : OpeningsRendition array
}