module BookingApi.Renditions

open System

[<CLIMutable>]
type MakeReservationRendition = { 
        Name : string
        Date : DateTimeOffset
        Email : string
        Quantity : int 
    }

