namespace RevitApiEntityMapper.tests
open NUnit.Framework
open ReflectedClasses
open Helpers
open System
open Autodesk.Revit.DB;
open Autodesk.Revit.DB.ExtensibleStorage
open System.Collections.Generic
open System.Linq

[<TestFixture>]
type GetterTests () = 

    [<SetUp>]
    member this.SetUp() = setUp()

    [<Test>]
    member this.ShouldGetValue () =
        testGetterWith<Bool>
            (fun schema factory -> let e = Entity(schema)
                                   e.Set<bool>("Some",true)
                                   let wall = getWall ()
                                   execInTransaction (fun () -> wall.SetEntity e |> ignore)
                                   (factory (wall.GetEntity schema)).Some |> assertThat true)

    [<Test>]
    member this.ShouldGetIncludedEntity () =
        testGetterWith<IncludedEntity>
            (fun schema factory -> let e = Entity(schema)
                                   let included = Entity(Guid("56f48442-ae75-4a01-98c6-a3b6a4d4bc8f"))
                                   included.Set<bool>("Some",true)
                                   e.Set<Entity>("Some",included)
                                   let wall = getWall ()
                                   execInTransaction (fun () -> wall.SetEntity e |> ignore)
                                   (factory (wall.GetEntity schema)).Some.Some |> assertThat true)
    
    [<Test>]
    member this.ShouldSetNullThenInvalidEntity () =
        testGetterWith<IncludedEntity> 
            (fun schema factory -> let e = Entity(schema)
                                   let included = Entity()
                                   e.Set<Entity>("Some",included)
                                   let wall = getWall()
                                   execInTransaction (fun () -> wall.SetEntity e |> ignore)
                                   (factory (wall.GetEntity schema)).Some |> assertThat null)

    [<Test>]
    member this.ShouldGetRecursiveEntity () =
        testGetterWith<Recursive>
            (fun schema factoty -> let e = Entity(schema)
                                   let wall = getWall()
                                   execInTransaction (fun () -> wall.SetEntity e |> ignore)
                                   (factoty (wall.GetEntity schema)).Some |> assertThat null)

    [<Test>]
    member this.ShouldGetListValues () =
        testGetterWith<List> 
             (fun schema factory -> let e = Entity(schema)
                                    e.Set<IList<bool>>("Some",List<bool>(seq{true}))
                                    let wall = getWall ()
                                    execInTransaction (fun () -> wall.SetEntity e |> ignore)
                                    (factory (wall.GetEntity schema)).Some.First() |> assertThat true)

    [<Test>]
    member this.ShouldGetNonSettedCollection () = 
        testGetterWith<ElementTree>
            (fun schema factory -> let e = Entity(schema)
                                   e.Set("RootElement",ElementId(5))
                                   let wall = getWall ()
                                   execInTransaction (fun () -> wall.SetEntity e |> ignore)
                                   (factory (wall.GetEntity schema)).DependentElements.Count |> assertThat 0)

    [<Test>]
    member this.ShouldGetListWithIncludedEntity () =
        testGetterWith<IncludedEntityList>
            (fun schema factory -> let e = Entity(schema)
                                   let included = Entity(Guid("56f48442-ae75-4a01-98c6-a3b6a4d4bc8f"))
                                   included.Set<bool>("Some",true)
                                   e.Set<IList<Entity>>("Some", List<Entity>(seq{included}))
                                   let wall = getWall ()
                                   execInTransaction (fun () -> wall.SetEntity e |> ignore)
                                   (factory (wall.GetEntity schema)).Some.First().Some |> assertThat true)

    [<Test>]
    member this.ShouldGetDictValues () =
        testGetterWith<Dictionary> 
             (fun schema factory -> let e = Entity(schema)
                                    let dict = Dictionary<string,bool>()
                                    dict.Add("key",true)
                                    e.Set<IDictionary<string,bool>>("Some",dict)
                                    let wall = getWall ()
                                    execInTransaction (fun () -> wall.SetEntity e |> ignore)
                                    let pair = (factory (wall.GetEntity schema)).Some.First()
                                    pair.Key |> assertThat "key"
                                    pair.Value |> assertThat true)

    [<Test>]
    member this.ShouldGetDictWithIncludedEntity () =
        testGetterWith<IncludedEntityInDictionary> 
             (fun schema factory -> let e = Entity(schema)
                                    let included = Entity(Guid("56f48442-ae75-4a01-98c6-a3b6a4d4bc8f"))
                                    included.Set<bool>("Some",true)
                                    let dict = Dictionary<string,Entity>()
                                    dict.Add("key",included)
                                    e.Set<IDictionary<string,Entity>>("Some",dict)
                                    let wall = getWall ()
                                    execInTransaction (fun () -> wall.SetEntity e |> ignore)
                                    let pair = (factory (wall.GetEntity schema)).Some.First()
                                    pair.Key |> assertThat "key"
                                    pair.Value.Some |> assertThat true)