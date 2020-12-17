namespace RevitApiEntityMapper.tests
open NUnit.Framework
open ReflectedClasses
open Helpers
open Autodesk.Revit.DB.ExtensibleStorage
open TypeResolver
open System

[<TestFixture>]
type CreateTests() = 

    [<SetUp>]
    member this.SetUp() = setUp()
    
    [<Test>]
    member this.ShouldCreateSimpleSchema() =
        typeof<Bool> |> testCreatorWith 
                        (fun s-> hasType s boolT)

    [<Test>]
    member this.ShouldCreateRecursive () = 
        typeof<Recursive> |> testCreatorWith
                            (fun s -> hasType s typeofEntity)
    
    [<Test>]
    member this.ShouldCreateWithIncludedEntity() =
        typeof<IncludedEntity> |> testCreatorWith 
                                  (fun s->let included = Schema.Lookup(Guid("56f48442-ae75-4a01-98c6-a3b6a4d4bc8f"))
                                          hasType s typeofEntity
                                          hasType included boolT)

    [<Test>]
    member this.ShouldCreateIList () =
        typeof<List> |> testCreatorWith 
                       (fun s-> hasType s boolT)

    [<Test>]
    member this.ShouldCreateDict () =
        typeof<Dictionary> |> testCreatorWith 
                        (fun s -> hasTypes s (strT,boolT))

    [<Test>]
    member this.ShouldCreateDictWithIncludedEntities () =
        typeof<IncludedEntityInDictionary> |> testCreatorWith
                                               (fun s-> hasTypes s (strT,typeofEntity))

    [<Test>]
    member this.ShouldCreateListWithIncludedEntities () =
        typeof<IncludedEntityList> |> testCreatorWith
                                               (fun s -> hasType s typeofEntity)

    [<Test>]
    member this.ShouldCreateSchemaWithTwoEqualsIncluededSchemas () =
        typeof<SameIncludedEntity> |> testCreatorWith 
                                               (fun s -> hasType s typeofEntity)
        