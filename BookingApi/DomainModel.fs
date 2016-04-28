namespace BookingApi.Domain

open BookingApi.Messages
open System
open System.Collections

module Reservations =
    
    type IReservations =
        inherit seq<Envelope<Reservation>>
        abstract Between : DateTime -> DateTime -> seq<Envelope<Reservation>>

    type InMemoryReservations(reservations) =
        interface IReservations with
            member this.Between min max =
                reservations
                |> Seq.filter (fun r -> min <= r.Item.Date && r.Item.Date <= max)
            member this.GetEnumerator() = 
                reservations.GetEnumerator()
            member this.GetEnumerator() = 
                reservations.GetEnumerator() :> IEnumerator

    let ToReservations reservations = 
        InMemoryReservations(reservations)

    let Between min max (reservations : IReservations) =
        reservations.Between min max

    let On (date : DateTime) reservations = 
        let min = date.Date
        let max = (min.AddDays 1.0) - TimeSpan.FromTicks 1L
        reservations |> Between min max

    let Handle capacity reservations (request : Envelope<MakeReservation>) =
        let nbReservedSeatsOnDate =
            reservations
            |> On request.Item.Date
            |> Seq.sumBy (fun r -> r.Item.Quantity)
        if capacity - nbReservedSeatsOnDate < request.Item.Quantity then
            None
        else
            {
                Date = request.Item.Date
                Name = request.Item.Name
                Email = request.Item.Email
                Quantity = request.Item.Quantity
            }
            |> EnvelopWithDefaults
            |> Some

module Notifications =
    type INotifications =
        inherit seq<Envelope<Notification>>
        abstract About : Guid -> seq<Envelope<Notification>>

    type InMemoryNotifications(notifications) =
        interface INotifications with
            member this.About id = 
                notifications
                |> Seq.filter(fun n -> n.Item.About = id)
            member this.GetEnumerator() = notifications.GetEnumerator()
            member this.GetEnumerator() = notifications.GetEnumerator() :> IEnumerator

    let ToNotifications notifications = InMemoryNotifications(notifications)
    let About id (notifications : INotifications) =
        notifications.About id