namespace BookingApi.WebHost

open System
open System.Collections.Concurrent
open System.Reactive
open System.Web.Http
open FSharp.Reactive
open BookingApi.Infrastructure
open BookingApi.Messages
open BookingApi.Domain.Reservations
open BookingApi.Domain.Notifications

type Agent<'T> = MailboxProcessor<'T>

type Global() =
    inherit System.Web.HttpApplication()
    member this.Application_Start (sender : obj) (e : EventArgs) =
        let maximumCapacity = 10
        let reservations = ConcurrentBag<Envelope<Reservation>>()
        let notifications = ConcurrentBag<Envelope<Notification>>()
        
        let reservationSubject = new Subjects.Subject<Envelope<Reservation>>()
        reservationSubject.Subscribe reservations.Add |> ignore

        let notificationsSubject = new Subjects.Subject<Notification>()
        notificationsSubject
        |> Observable.map EnvelopWithDefaults
        |> Observable.subscribe notifications.Add ignore ignore
        |> ignore

        let agent = new Agent<Envelope<MakeReservation>>(fun inbox ->
            let rec loop () = 
                async{
                    let! command = inbox.Receive()
                    let res = reservations |> ToReservations
                    let handle = Handle maximumCapacity res
                    let newReservation = handle command
                    match newReservation with
                    | Some(r) -> 
                        reservationSubject.OnNext r
                        notificationsSubject.OnNext
                            {
                                About = command.Id
                                Type = "Success"
                                Message = 
                                    sprintf
                                        "Your reservation for %s was completed. See you soon!"
                                        (command.Item.Date.ToString "dd/MM/yyyy")
                            }
                    | None -> 
                        notificationsSubject.OnNext
                            {
                                About = command.Id
                                Type = "Failure"
                                Message = 
                                    sprintf
                                        "We are sorry to inform you that your reservation for %s could not be completed."
                                        (command.Item.Date.ToString "dd/MM/yyyy")
                            }
                    return! loop()
                }
            loop())
        do agent.Start()

        Configure 
            (reservations |> ToReservations)
            (notifications |> ToNotifications)
            (Observer.Create agent.Post)
            maximumCapacity
            GlobalConfiguration.Configuration
            