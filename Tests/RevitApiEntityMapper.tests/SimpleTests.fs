namespace RevitApiEntityMapper.tests
open NUnit.Framework
open RTF.Framework
open RTF.Applications
open FluentAssertions
open ReflectedClasses
open FieldMapper
open Abstractions
open Helpers
open System.Reflection
open System.IO

[<TestFixture>]
type SimpleTests() = 

    [<SetUp>]
    member this.SetUp() = setUp ()
    
    [<Test>]
    member this.ShouldCreateSimpleSchema() =
        match fieldMapper typeof<Bool> with
            |Success(s) -> Assert.That(s.GUID.ToString(),Is.EqualTo("e7fd718b-d7f5-4dce-ac33-06e138749344"))|> ignore
            |Failure(s) -> failwith(s)

