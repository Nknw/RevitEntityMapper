namespace RevitApiEntityMapper.tests
open NUnit.Framework
open ReflectedClasses
open Helpers
open System
open Autodesk.Revit.DB.ExtensibleStorage

[<TestFixture>]
type GetterTests () = 

    [<SetUp>]
    member this.SetUp() = setUp()

    [<Test>]
    member this.ShouldGetValueProp () =
        typeof<Bool> 
         |> testGetterWith 
            (fun (func,schema) -> let factory = func :?> Entity -> Bool 
                                  let e = Entity(schema)
                                  e.Set<bool>("Some",true)
                                  let wall = getWall ()
                                  execInTransaction (fun () -> wall.SetEntity(e) |> ignore)
                                  (factory (wall.GetEntity(schema))).Some |> assertThat true)

    [<Test>]
    member this.ShouldGetIncludedEntity () =
        typeof<IncludedEntity>
         |> testGetterWith
            (fun (func,schema) -> let factory = func :?> Entity -> IncludedEntity 
                                  let e = Entity(schema)
                                  let included = Entity(Guid("56f48442-ae75-4a01-98c6-a3b6a4d4bc8f"))
                                  included.Set<bool>("Some",true)
                                  e.Set<Entity>("Some",included)
                                  let wall = getWall ()
                                  execInTransaction (fun () -> wall.SetEntity(e) |> ignore)
                                  (factory (wall.GetEntity(schema))).Some.Some |> assertThat true)