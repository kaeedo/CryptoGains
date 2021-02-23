namespace CryptoGains.Console

open System
open System.Net.Http
open FsToolkit.ErrorHandling
open Oryx
open Oryx.ThothJsonNet.ResponseReader
open Thoth.Json.Net

[<RequireQualifiedAccess>]
module GitHub =
    [<Literal>]
    let private releaseUrl = "https://api.github.com/repos/kaeedo/CryptoGains/releases/latest"
    
    let private releaseDecoder =
        Decode.field "tag_name" Decode.string
    
    let getLatestVersion () =
        taskResult {
            use client = new HttpClient()
            
            let ctx =
                Context.defaultContext
                |> Context.withHttpClient client
                
            let! latestRelease =
                GET
                >=> withUrl releaseUrl
                >=> fetch
                >=> json releaseDecoder
                |> runAsync ctx
                
            return Version(latestRelease)
        }

