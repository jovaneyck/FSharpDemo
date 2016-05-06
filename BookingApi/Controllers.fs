namespace BookingApi.HttpApi

open System
open System.Net
open System.Net.Http
open System.Web.Http
open System.Reactive.Subjects
open BookingApi.Renditions
open BookingApi.Messages
open BookingApi.Domain.Notifications
open BookingApi.Domain

type HomeController() =
    inherit ApiController()
    member this.Get() = 
        this.Request.CreateResponse(
            HttpStatusCode.OK,
            "Hello world!")

type ReservationsController() =
    inherit ApiController()

    let subject = new Subject<Envelope<MakeReservation>>()
    interface IObservable<Envelope<MakeReservation>> with
            member this.Subscribe observer = subject.Subscribe observer
    override this.Dispose disposing =
        if disposing then subject.Dispose()
        base.Dispose disposing

    member this.Post(rendition : MakeReservationRendition) =
        let cmd : MakeReservation = 
            {
                Name = rendition.Name
                Date = rendition.Date |> DateTime.Parse
                Email = rendition.Email
                Quantity = rendition.Quantity
            }
        let env = cmd |> EnvelopWithDefaults
            
        subject.OnNext env

        this.Request.CreateResponse(
            HttpStatusCode.Accepted,
            {
                Links = 
                    [| {
                        Rel = "http://my.app/notification"
                        Href = "/notifications/"  + env.Id.ToString()
                    } |]
            })

type NotificationsController(notifications : INotifications) =
    inherit ApiController()

    member this.Get id =
        let toRendition (n: Envelope<Notification>) : NotificationRendition = {
            About = n.Item.About.ToString()
            Type = n.Item.Type
            Message = n.Item.Message
        }
        let matches = 
            notifications 
            |> About id
            |> Seq.map toRendition
            |> Seq.toArray

        this.Request.CreateResponse(
            HttpStatusCode.OK,
            { Notifications = matches })

type AvailabilityController(reservations : Reservations.IReservations,
                            seatingCapacity : int) =
    inherit ApiController()

    let dateFormat = "dd/MM/yyyy"
    let today = DateTimeOffset.Now.Date

    let getAvailableSeats map date =
        if map |> Map.containsKey date then
            seatingCapacity - (map |> Map.find date)
        else
            seatingCapacity

    member this.Get year =
        let (min,max) = Dates.BoundariesOff(Year(year))
        let reservationsInPeriod = 
            reservations
            |> Reservations.Between min max
            |> Seq.groupBy (fun r -> r.Item.Date)
            |> Seq.map (fun (d,rs) -> 
                (d, rs |> Seq.sumBy(fun r->r.Item.Quantity)))
            |> Map.ofSeq

        let availabilities =
            Dates.In (Year(year))
            |> Seq.map (fun d ->
                {
                    Date = d.ToString dateFormat
                    FreeSeats = 
                        if d < today then 
                            0 
                        else 
                            getAvailableSeats reservationsInPeriod d
                })
            |> Seq.toArray
        this.Request.CreateResponse(
            HttpStatusCode.OK,
            { Openings = availabilities })
    
    member this.Get(year, month) =
        let (min,max) = Dates.BoundariesOff(Month(year, month))
        let reservationsInPeriod = 
            reservations
            |> Reservations.Between min max
            |> Seq.groupBy (fun r -> r.Item.Date)
            |> Seq.map (fun (d,rs) -> 
                (d, rs |> Seq.sumBy(fun r->r.Item.Quantity)))
            |> Map.ofSeq
        let availabilities =
            Dates.In (Month(year, month))
            |> Seq.map (fun d ->
                {
                    Date = d.ToString dateFormat
                    FreeSeats = 
                        if d < today then 
                            0 
                        else 
                            getAvailableSeats reservationsInPeriod d
                })
            |> Seq.toArray
        this.Request.CreateResponse(
            HttpStatusCode.OK,
            { Openings = availabilities })
    member this.Get(year, month, day) =
        let (min,max) = Dates.BoundariesOff(Day(year, month, day))
        let reservationsInPeriod = 
            reservations
            |> Reservations.Between min max
            |> Seq.groupBy (fun r -> r.Item.Date)
            |> Seq.map (fun (d,rs) -> 
                (d, rs |> Seq.sumBy(fun r->r.Item.Quantity)))
            |> Map.ofSeq
        let availabilities =
            Dates.In (Day(year, month, day))
            |> Seq.map (fun d ->
                {
                    Date = d.ToString dateFormat
                    FreeSeats = 
                        if d < today then 
                            0 
                        else 
                            getAvailableSeats reservationsInPeriod d
                })
            |> Seq.toArray
        this.Request.CreateResponse(
            HttpStatusCode.OK,
            { Openings = availabilities })