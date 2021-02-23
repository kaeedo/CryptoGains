namespace CryptoGains.Console.Configuration

open System
open System.IO
open CryptoGains
open Spectre.Console.Cli
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
    
//type CommandLine() =
//    inherit CommandSettings()
//    
//    [<CommandOption("-v|--version")>]
//    member val Version = false with get, set
//    
//    [<CommandOption("-u|--update")>]
//    member val ForceUpdate = false with get, set
//    
//type CommandLineCommand() =
//    inherit Command<CommandLine>()
//    override this.Execute(context: CommandContext, settings: CommandLine) =
//        settings.Version, settings.ForceUpdate
        

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

    member this.GetConfiguration() =
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
