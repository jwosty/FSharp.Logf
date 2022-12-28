// some Mocha extensions to increase feature parity with Expecto
module Fable.Mocha.Expect
open Fable.Mocha

let inline sequenceEqual (actual: 'a seq) (expected: 'a seq) (msg: string) = Expect.equal actual expected msg
let throws (f: unit -> unit) (msg: string) : unit =
    let pass =
        try
            f ()
            false
        with e -> true
    if not pass then failwithf "%s" msg
// Fable doesn't use custom exceptions, so this isn't something you can do in JS anyway
let throwsT<'TExn when 'TExn :> System.Exception> (f: unit -> unit) (msg: string) : unit = throws f msg