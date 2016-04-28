namespace BookingApi.HttpApi

open System
open System.Net
open System.Net.Http
open System.Web.Http
open BookingApi.Renditions

type HomeController() =
    inherit ApiController()
    member this.Get() = 
        this.Request.CreateResponse(
            HttpStatusCode.OK,
            "Hello world!")

type ReservationController() =
    inherit ApiController()
    member this.Post(rendition : MakeReservationRendition) =
        new HttpResponseMessage(HttpStatusCode.Accepted)