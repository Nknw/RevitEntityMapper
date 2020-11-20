namespace RevitApiEntityMapper.tests
open NUnit.Framework
open ReflectedClasses
open Helpers
open Autodesk.Revit.DB.ExtensibleStorage
open Abstractions

[<TestFixture>]
type CreateTests() = 

    [<SetUp>]
    member this.SetUp() = setUp ()
    
    [<Test>]
    member this.ShouldCreateSimpleSchema() =
        typeof<Bool> |> testCreatorWith 
                        (fun s-> hasType s boolT)
    
    [<Test>]
    member this.ShouldCreateWithEncludedEntity() =
        typeof<IncludedEntity> |> testCreatorWith 
                                  (fun s->let included = Schema.Lookup(fstGuid)
                                          hasType s typeofEntity
                                          hasType included boolT)

    [<Test>]
    member this.ShouldCreateIList () =
        typeof<Lst> |> testCreatorWith 
                       (fun s-> hasType s boolT)

    [<Test>]
    member this.ShouldCreateDict () =
        typeof<Dict> |> testCreatorWith 
                        (fun s -> hasTypes s (strT,boolT))

    [<Test>]
    member this.ShouldCreateDictWithEncludedEntities () =
        typeof<IncludedEntityInDictionary> |> testCreatorWith
                                               (fun s-> hasTypes s (strT,typeofEntity))

    [<Test>]
    member this.ShouldCreateListWithEncludedEntities () =
        typeof<IncludedEntityList> |> testCreatorWith
                                               (fun s -> hasType s typeofEntity)
        