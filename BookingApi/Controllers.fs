namespace BookingApi.HttpApi

open System
open System.Net
open System.Net.Http
open System.Web.Http
open System.Reactive.Subjects
open BookingApi.Renditions
open BookingApi.Messages

type HomeController() =
    inherit ApiController()
    member this.Get() = 
        this.Request.CreateResponse(
            HttpStatusCode.OK,
            "Hello world!")

type ReservationController() =
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
            
        subject.OnNext (cmd |> EnvelopWithDefaults)

        new HttpResponseMessage(HttpStatusCode.Accepted)