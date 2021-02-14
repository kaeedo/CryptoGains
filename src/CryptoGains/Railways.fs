namespace CryptoGains

module Railways =
    /// convert a single value into a two-track result
    let succeed x = Result.Ok x

    /// convert a single value into a two-track result
    let fail x = Result.Error x

    /// apply either a success function or failure function
    let either successFunc failureFunc twoTrackInput =
        match twoTrackInput with
        | Result.Ok s -> successFunc s
        | Result.Error f -> failureFunc f

    /// convert a switch function into a two-track function
    let bind f = either f fail

    /// pipe a two-track value into a switch function
    // let (>>=) x f = bind f x

    /// compose two switches into another switch
    // let (>=>) s1 s2 = s1 >> bind s2

    /// convert a one-track function into a switch
    let switch f = f >> succeed

    /// convert a one-track function into a two-track function
    let map f = either (f >> succeed) fail

    /// convert a dead-end function into a one-track function
    let tee f x =
        f x
        x

    /// convert a one-track function into a switch with exception handling
    let tryCatch f exnHandler x =
        try
            f x |> succeed
        with ex -> exnHandler ex |> fail

    /// convert two one-track functions into a two-track function
    let doubleMap successFunc failureFunc =
        either (successFunc >> succeed) (failureFunc >> fail)

    /// add two switches in parallel
    let plus addSuccess addFailure switch1 switch2 x =
        match (switch1 x), (switch2 x) with
        | Result.Ok s1, Result.Ok s2 -> Result.Ok(addSuccess s1 s2)
        | Result.Error f1, Result.Ok _ -> Result.Error f1
        | Result.Ok _, Result.Error f2 -> Result.Error f2
        | Result.Error f1, Result.Error f2 -> Result.Error(addFailure f1 f2)


