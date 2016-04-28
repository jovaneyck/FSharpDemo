namespace BookingApi.WebHost

open System
open System.Collections.Concurrent
open System.Reactive
open System.Web.Http
open FSharp.Reactive
open BookingApi.Infrastructure
open BookingApi.Messages
open BookingApi.Domain.Reservations

type Agent<'T> = MailboxProcessor<'T>

type Global() =
    inherit System.Web.HttpApplication()
    member this.Application_Start (sender : obj) (e : EventArgs) =
        let maximumCapacity = 10
        let reservations = ConcurrentBag<Envelope<Reservation>>()

        let reservationSubject = new Subjects.Subject<Envelope<Reservation>>()
        reservationSubject.Subscribe reservations.Add |> ignore

        let agent = new Agent<Envelope<MakeReservation>>(fun inbox ->
            let rec loop () = 
                async{
                    let! command = inbox.Receive()
                    let res = reservations |> ToReservations
                    let handle = Handle maximumCapacity res
                    let newReservation = handle command
                    match newReservation with
                    | Some(r) -> reservationSubject.OnNext r
                    | None -> ()
                    return! loop()
                }
            loop())
        do agent.Start()

        Configure 
            (reservations |> ToReservations)
            (Observer.Create agent.Post)
            GlobalConfiguration.Configuration
            