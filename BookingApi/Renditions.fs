module BookingApi.Renditions

open System

[<CLIMutable>]
type MakeReservationRendition = { 
        Name : string
        Date : string
        Email : string
        Quantity : int 
    }