namespace RevitApiEntityMapper.tests
open NUnit.Framework
open ReflectedClasses
open Helpers
open System
open Autodesk.Revit.DB.ExtensibleStorage
open System.Collections.Generic
open System.Linq

[<TestFixture>]
type GetterTests () = 

    [<SetUp>]
    member this.SetUp() = setUp()

    [<Test>]
    member this.ShouldGetValue () =
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

    [<Test>]
    member this.ShouldGetListValues () =
        typeof<Lst>
         |> testGetterWith 
             (fun (func,schema) ->let factory = func :?> Entity -> Lst 
                                  let e = Entity(schema)
                                  e.Set<IList<bool>>("Some",List<bool>(seq{true}))
                                  let wall = getWall ()
                                  execInTransaction (fun () -> wall.SetEntity(e) |> ignore)
                                  (factory (wall.GetEntity(schema))).Some.First() |> assertThat true)

    [<Test>]
    member this.ShouldGetListWithIncludedEntity () =
        typeof<IncludedEntityList>
         |> testGetterWith
            (fun (func,schema) -> let factory = func :?> Entity -> IncludedEntityList
                                  let e = Entity(schema)
                                  let included = Entity(Guid("56f48442-ae75-4a01-98c6-a3b6a4d4bc8f"))
                                  included.Set<bool>("Some",true)
                                  e.Set<IList<Entity>>("Some", List<Entity>(seq{included}))
                                  let wall = getWall ()
                                  execInTransaction (fun () -> wall.SetEntity(e) |> ignore)
                                  (factory (wall.GetEntity(schema))).Some.First().Some |> assertThat true)

    [<Test>]
    member this.ShouldGetDictValues () =
        typeof<Dict>
         |> testGetterWith 
             (fun (func,schema) ->let factory = func :?> Entity -> Dict 
                                  let e = Entity(schema)
                                  let dict = Dictionary<string,bool>()
                                  dict.Add("key",true)
                                  e.Set<IDictionary<string,bool>>("Some",dict)
                                  let wall = getWall ()
                                  execInTransaction (fun () -> wall.SetEntity(e) |> ignore)
                                  let pair = (factory (wall.GetEntity(schema))).Some.First()
                                  pair.Key |> assertThat "key"
                                  pair.Value |> assertThat true)

    [<Test>]
    member this.ShouldGetDictWithIncludedEntity () =
        typeof<IncludedEntityInDictionary>
         |> testGetterWith 
             (fun (func,schema) ->let factory = func :?> Entity -> IncludedEntityInDictionary 
                                  let e = Entity(schema)
                                  let included = Entity(Guid("56f48442-ae75-4a01-98c6-a3b6a4d4bc8f"))
                                  included.Set<bool>("Some",true)
                                  let dict = Dictionary<string,Entity>()
                                  dict.Add("key",included)
                                  e.Set<IDictionary<string,Entity>>("Some",dict)
                                  let wall = getWall ()
                                  execInTransaction (fun () -> wall.SetEntity(e) |> ignore)
                                  let pair = (factory (wall.GetEntity(schema))).Some.First()
                                  pair.Key |> assertThat "key"
                                  pair.Value.Some |> assertThat true)