namespace BookingApi.WebHost

open System
open System.Web.Http

open BookingApi.Infrastructure
open BookingApi.Messages

type Global() =
    inherit System.Web.HttpApplication()
    member this.Application_Start (sender : obj) (e : EventArgs) =
        Configure 
            (System.Collections.Concurrent.ConcurrentBag<Envelope<Reservation>>())
            GlobalConfiguration.Configuration