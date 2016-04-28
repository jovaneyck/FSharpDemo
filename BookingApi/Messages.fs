namespace BookingApi.Messages

open System

[<CLIMutable>]
type MakeReservation = {
    Name : string
    Date : DateTime
    Email : string
    Quantity : int 
}

[<AutoOpen>]
module Envelope =
    
    [<CLIMutable>]
    type Envelope<'T> = {
        Id : Guid
        Created : DateTimeOffset
        Item : 'T
    }

    let Envelop id createdOn item = {
            Id = id
            Created = createdOn
            Item = item
        }

    let EnvelopWithDefaults item =
        Envelop (Guid.NewGuid()) DateTimeOffset.UtcNow item

[<CLIMutable>]
type Reservation = {
    Name : string
    Date : DateTime
    Email : string
    Quantity : int 
}

[<CLIMutable>]
type Notification = {
    About : Guid
    Type : string
    Message : string
}