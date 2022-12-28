module Fable.Mocha.Flip
open Fable.Mocha
open System

module Expect =
    let inline equal (msg: string) (expected: 'a) (actual: 'a) = Expect.equal actual expected msg
    let notEqual (msg: string) (expected: 'a) (actual: 'a) = Expect.notEqual actual expected msg
    let isTrue (msg: string) (actual: bool) = Expect.isTrue actual msg
    let isFalse (msg: string) (actual: bool) = Expect.isFalse actual msg
    let inline sequenceEqual (msg: string) (expected: 'a seq) (actual: 'a seq) =
        let actual' = Seq.toArray actual
        let expected' = Seq.toArray expected
        Expect.sequenceEqual actual' expected' msg
    let throws (msg: string) (f: unit -> unit) = Expect.throws f msg
    let throwsT<'TExn> (msg: string) (f: unit -> unit) = Expect.throwsT f msg