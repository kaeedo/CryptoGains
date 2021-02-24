namespace CryptoGains.Console.Configuration

open System
open System.IO
open CryptoGains
open Spectre.Console
open Thoth.Json.Net

type WithdrawnCryptocoin =
    { Cryptocoin: Cryptocoin
      AmountHeld: decimal }

type ExternallyHeldCryptocoin =
    { Cryptocoin: Cryptocoin
      AmountHeld: decimal
      PricePaid: Currency * decimal }

type ConfigurationFile =
    { WithdrawnCryptocoins: WithdrawnCryptocoin list
      ExternallyHeld: ExternallyHeldCryptocoin list
      ApiKey: string
      IsConfigured: bool }

type Configuration() =
    let mutable apiKey = String.Empty
    let mutable withdrawnCryptocoins: WithdrawnCryptocoin list = []
    let mutable externallyHeld: ExternallyHeldCryptocoin list = []
    let mutable isConfigured = false

    let configurationFile = "./cryptoGainsConfig.json"

    let extra = Extra.empty |> Extra.withDecimal

    let readConfig config =
        let config =
            Decode.Auto.unsafeFromString<ConfigurationFile> (config, extra = extra, caseStrategy = CamelCase)

        apiKey <- config.ApiKey
        externallyHeld <- config.ExternallyHeld
        withdrawnCryptocoins <- config.WithdrawnCryptocoins
        isConfigured <- config.IsConfigured

    do
        if not (File.Exists(configurationFile)) then
            let config =
                { ConfigurationFile.ApiKey = String.Empty
                  WithdrawnCryptocoins = List.Empty
                  ExternallyHeld = List.Empty
                  IsConfigured = false }

            let json =
                Encode.Auto.toString (4, config, extra = extra, caseStrategy = CamelCase)

            File.WriteAllText(configurationFile, json)

        let loadedConfig = File.ReadAllText(configurationFile)
        readConfig loadedConfig

    member this.SetApiKey value = apiKey <- value

    member this.SetWithdrawnCryptocoins value = withdrawnCryptocoins <- value

    member this.SetExternallyHeld value = externallyHeld <- value

    member this.ResetConfiguration() =
        apiKey <- String.Empty
        withdrawnCryptocoins <- []
        externallyHeld <- []
        isConfigured <- false

    member this.GetConfiguration() =
        let apiKey =
            if String.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("BITPANDA")) then
                let apiKeyFromConfig = 
                    if String.IsNullOrWhiteSpace(apiKey)
                    then AnsiConsole.Ask<string>("BitPanda API Key: ")
                    else apiKey
                
                apiKey <- apiKeyFromConfig
                apiKey
            else
                Environment.GetEnvironmentVariable("BITPANDA")
                
        { ConfigurationFile.ApiKey = apiKey
          WithdrawnCryptocoins = withdrawnCryptocoins
          ExternallyHeld = externallyHeld
          IsConfigured = isConfigured }

    member this.WriteConfiguration() =
        let config =
            { ConfigurationFile.ApiKey = apiKey
              WithdrawnCryptocoins = withdrawnCryptocoins
              ExternallyHeld = externallyHeld
              IsConfigured = true }

        let json =
            Encode.Auto.toString (4, config, extra = extra, caseStrategy = CamelCase)

        File.WriteAllText(configurationFile, json)
