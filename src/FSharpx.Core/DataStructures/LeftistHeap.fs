﻿// originally published by Julien
// original implementation taken from http://lepensemoi.free.fr/index.php/2009/12/03/leftist-heap
//J.F. modified

namespace FSharpx.DataStructures

open System.Collections
open System.Collections.Generic

type LeftistHeap<'a when 'a : comparison> =
    | E of bool
    | T of bool * int * int * 'a * LeftistHeap<'a> * LeftistHeap<'a> 

    with

    static member private rank : LeftistHeap<'a> -> int = function 
        | E(_) -> 0 
        | T(_, _, r, _, _, _) -> r

    static member private make (x: 'a) (a: LeftistHeap<'a>) (b: LeftistHeap<'a>) : LeftistHeap<'a> =
        if LeftistHeap.rank a > LeftistHeap.rank b then
          T((a.IsMaximalist), (a.Length + b.Length + 1), LeftistHeap.rank b + 1, x, a, b)
        else
          T((a.IsMaximalist), (a.Length + b.Length + 1), LeftistHeap.rank a + 1, x, b, a)

    static member private merge (h1: LeftistHeap<'a>) (h2: LeftistHeap<'a>) : LeftistHeap<'a> = 
        if (h1.IsMaximalist) = (h2.IsMaximalist) then
            match h1, h2 with
            | E(_), x | x, E(_) -> x
            | T(_, _, _, x, a1, b1), T(_, _, _, y, a2, b2) ->
                if (h1.IsMaximalist) then
                    if x < y then LeftistHeap.make y a2 (LeftistHeap.merge h1 b2)
                    else LeftistHeap.make x a1 (LeftistHeap.merge b1 h2)
                else
                    if x < y then LeftistHeap.make x a1 (LeftistHeap.merge b1 h2)
                    else LeftistHeap.make y a2 (LeftistHeap.merge h1 b2)
        else
            failwith "not same max or min"

    //http://lorgonblog.wordpress.com/2008/04/06/catamorphisms-part-two
    static member private foldHeap nodeF leafV (h : LeftistHeap<'a>) = 

        let rec Loop (h : LeftistHeap<'a>) cont = 
            match h with 
            | T(_, _, _, a, l, r) -> Loop l  (fun lacc ->  
                                    Loop r (fun racc -> 
                                    cont (nodeF a lacc racc))) 
            | E(_) -> cont leafV 
        Loop h (fun x -> x)

    static member private inOrder (h : LeftistHeap<'a>) = (LeftistHeap.foldHeap (fun x l r acc -> l (x :: (r acc))) (fun acc -> acc) h) [] 
           
    static member private isEmpty : LeftistHeap<'a> -> bool = function 
        | E(_) -> true 
        | _ -> false

    static member private tryMerge (h1: LeftistHeap<'a>) (h2: LeftistHeap<'a>) : LeftistHeap<'a> option = 
        if (h1.IsMaximalist) = (h2.IsMaximalist) then
            match h1, h2 with
            | E(_), x | x, E(_) -> Some(x)
            | T(_, _, _, x, a1, b1), T(_, _, _, y, a2, b2) ->
                if x < y then Some(LeftistHeap.make x a1 (LeftistHeap.merge b1 h2))
                else Some(LeftistHeap.make y a2 (LeftistHeap.merge h1 b2))
        else None

    static member private insert (x: 'a) (h: LeftistHeap<'a>) : LeftistHeap<'a> = 
        let isMaximalist = h.IsMaximalist
        LeftistHeap.merge (T(isMaximalist, 1, 1, x, E(isMaximalist), E(isMaximalist))) h

    static member private head: LeftistHeap<'a> -> 'a = function
        | E(_) -> raise Exceptions.Empty
        | T(_, _, _, x, _, _) -> x

    static member private tryGetHead: LeftistHeap<'a>  -> 'a option = function
        | E(_) -> None
        | T(_, _, _, x, _, _) -> Some(x)
 
    static member internal ofSeq (maximalist: bool) (s:seq<'a>) : LeftistHeap<'a> = 
        if Seq.isEmpty s then E(maximalist)
        else
            let x, _ = 
                Seq.fold (fun (acc, isMaximalist) elem -> (((T(isMaximalist, 1, 1, elem, E(isMaximalist), E(isMaximalist))))::acc), isMaximalist) ([], maximalist) s
    
            let pairWiseMerge (l: list<LeftistHeap<'a>>) =
                let rec loop (acc: list<LeftistHeap<'a>>) : list<LeftistHeap<'a>> -> list<LeftistHeap<'a>> = function
                    | h1::h2::tl -> loop ((LeftistHeap.merge h1 h2)::acc) tl
                    | h1::[] -> h1::acc
                    | [] -> acc

                loop [] l

            let rec loop : list<LeftistHeap<'a>> -> LeftistHeap<'a> = function
                | h::[] -> h
                | x -> loop (pairWiseMerge x)
                
            loop x             

    static member private tail : LeftistHeap<'a> -> LeftistHeap<'a> = function
        | E(_) -> raise Exceptions.Empty
        | T(_, _, _, _, a, b) -> LeftistHeap.merge a b

    static member private tryGetTail : LeftistHeap<'a> -> LeftistHeap<'a> option = function
        | E(_) -> None
        | T(_, _, _, _, a, b) -> Some(LeftistHeap.merge a b)

    static member private tryUncons (h :LeftistHeap<'a>) : ('a * LeftistHeap<'a>) option =
        match LeftistHeap.tryGetHead h with
            | None -> None
            | Some(x) -> Some(x, (LeftistHeap.tail h))
        
    ///returns the min or max element, O(1)
    member this.Head = LeftistHeap.head this

    ///returns option first min or max element, O(1)
    member this.TryGetHead = LeftistHeap.tryGetHead this

    ///returns a new heap with the element inserted, O(log n)
    member this.Insert x  = LeftistHeap.insert x this

    ///returns true if the heap has no elements, O(1)
    member this.IsEmpty = LeftistHeap.isEmpty this

    ///returns true if the heap has max element at head, O(1)
    member this.IsMaximalist : bool = 
        match this with
        | E(m) -> m 
        |  T(m, _, _, _, _, _) -> m

    ///returns the count of elememts, O(1)
    member this.Length : int = 
        match this with
        | E(_) -> 0
        | T(_, i, _, _, _, _) -> i

    ///returns heap from merging two heaps, both must have same isMaximalist, O(log n)
    member this.Merge xs = LeftistHeap.merge this xs

    ///returns heap option from merging two heaps, O(log n)
    member this.TryMerge xs = LeftistHeap.tryMerge this xs

    ///returns a new heap of the elements trailing the head, O(log n)
    member this.Tail = LeftistHeap.tail this
       
    ///returns option heap of the elements trailing the head, O(log n)
    member this.TryGetTail = LeftistHeap.tryGetTail this

    ///returns the head element and tail, O(log n)
    member this.Uncons() = 
        (LeftistHeap.head this), (LeftistHeap.tail this)

    ///returns option head element and tail, O(log n)
    member this.TryUncons() = LeftistHeap.tryUncons this

    interface IHeap<LeftistHeap<'a>, 'a> with
        
        member this.Count() = this.Length

        member this.Head() = LeftistHeap.head this

        member this.TryGetHead() = LeftistHeap.tryGetHead this

        member this.Insert (x : 'a) = LeftistHeap.insert x this

        member this.IsEmpty = LeftistHeap.isEmpty this

        member this.IsMaximalist = this.IsMaximalist 

        member this.Length() = this.Length 

        member this.Merge (xs : LeftistHeap<'a>) = LeftistHeap.merge this xs

        member this.TryMerge (xs : LeftistHeap<'a>)  = 
            match LeftistHeap.tryMerge this xs with
            | None -> None
            | Some(xs) -> Some(xs)

        member this.Tail() = LeftistHeap.tail this

        member this.TryGetTail() =
            match LeftistHeap.tryGetTail this with
            | None -> None
            | Some(xs) -> Some(xs)

        member this.Uncons() = 
            (LeftistHeap.head this), (LeftistHeap.tail this) 

        member this.TryUncons() =
            match LeftistHeap.tryUncons this with
            | None -> None
            | Some(x, xs) -> Some(x, xs)

        member this.GetEnumerator() = 
            let e = 
                if this.IsMaximalist
//WARNING! List.sort |> List.rev significantly faster (caveat: on 32-bit Win 7) than List.sortwith...go figure!
//            LeftistHeap.inOrder this |> List.sortWith (fun x y -> if (x > y) then -1
//                                                                             else 
//                                                                               if (x = y) then 0
//                                                                               else 1) |> List.fold f state
                then LeftistHeap.inOrder this |> List.sort |> List.rev |> List.toSeq
                else LeftistHeap.inOrder this |> List.sort |> List.toSeq

            e.GetEnumerator()

        member this.GetEnumerator() = (this :> _ seq).GetEnumerator() :> IEnumerator  

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module LeftistHeap =   
    //pattern discriminator

    let (|Cons|Nil|) (h: LeftistHeap<'a>) = match h.TryUncons() with Some(a,b) -> Cons(a,b) | None -> Nil
  
    ///returns a empty heap, O(1)
    let inline empty (maximalist: bool) = E(maximalist)

    ///returns the min or max element, O(1)
    let inline head (xs: LeftistHeap<'a>)  = xs.Head

    ///returns option first min or max element, O(1)
    let inline tryGetHead (xs: LeftistHeap<'a>)  = xs.TryGetHead

    ///returns a new heap with the element inserted, O(log n)
    let inline insert x (xs: LeftistHeap<'a>) = xs.Insert x   

    ///returns true if the heap has no elements, O(1)
    let inline isEmpty (xs: LeftistHeap<'a>) = xs.IsEmpty

    ///returns true if the heap has max element at head, O(1)
    let inline isMaximalist (xs: LeftistHeap<'a>) = xs.IsMaximalist

    ///returns the count of elememts, O(1)
    let inline length (xs: LeftistHeap<'a>) = xs.Length 

    ///returns heap from merging two heaps, both must have same isMaximalist, O(log n)
    let inline merge (xs: LeftistHeap<'a>) (ys: LeftistHeap<'a>) = xs.Merge ys

    ///returns heap option from merging two heaps, O(log n)
    let inline tryMerge (xs: LeftistHeap<'a>) (ys: LeftistHeap<'a>) = xs.TryMerge ys

    ///returns heap from the sequence, O(log n)
    let ofSeq maximalist s = LeftistHeap.ofSeq maximalist s

    ///returns a new heap of the elements trailing the head, O(log n)
    let inline tail (xs: LeftistHeap<'a>) = xs.Tail

    ///returns option heap of the elements trailing the head, O(log n)
    let inline tryGetTail (xs: LeftistHeap<'a>) = xs.TryGetTail

    ///returns the head element and tail, O(log n)
    let inline uncons (xs: LeftistHeap<'a>) = xs.Uncons()

    ///returns option head element and tail, O(log n)
    let inline tryUncons (xs: LeftistHeap<'a>) = xs.TryUncons()