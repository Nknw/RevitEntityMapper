namespace RevitApiEntityMapper.tests
open NUnit.Framework
open ReflectedClasses
open Helpers
open Autodesk.Revit.DB.ExtensibleStorage

[<TestFixture>]
type CreateTests() = 

    [<SetUp>]
    member this.SetUp() = setUp ()
    
    [<Test>]
    member this.ShouldCreateSimpleSchema() =
        typeof<Bool> |> testCreatorWith 
                        (fun s-> s.GUID |> assertFstGuid
                                 hasField s)
    
    [<Test>]
    member this.ShouldCreateWithEncludedEntity() =
        typeof<IncludedEntity> |> testCreatorWith 
                                  (fun s->s.GUID |> assertSndGuid 
                                          let incl = Schema.Lookup(fstGuid)
                                          assertFstGuid incl.GUID
                                          hasField s
                                          hasField incl)

    [<Test>]
    member this.ShouldCreateIList () =
        typeof<Lst> |> testCreatorWith 
                       (fun s-> s.GUID |> assertFstGuid
                                hasField s)

    [<Test>]
    member this.ShouldCreateDict () =
        typeof<Dict> |> testCreatorWith 
                        (fun s -> s.GUID |> assertFstGuid
                                  hasField s)

   // [<Test>]
   // member this.ShouldCreateDictWithEncludedEntities () =
        