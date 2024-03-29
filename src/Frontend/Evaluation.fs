﻿// Copyright (c) 2019 - for information on the respective copyright owner
// see the NOTICE file and/or the repository 
// https://github.com/blech-lang/blech.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.


module Blech.Frontend.Evaluation

open Constants
open CommonTypes

/// Unchecked arithmetic operators for Bits
type BitsUnchecked = 
    static member (+) (l: uint8, r: uint8) = l + r
    static member (-) (l: uint8, r: uint8) = l - r
    static member (*) (l: uint8, r: uint8) = l * r
    
    static member (+) (l: uint16, r: uint16) = l + r
    static member (-) (l: uint16, r: uint16) = l - r
    static member (*) (l: uint16, r: uint16) = l * r

    static member (+) (l: uint32, r: uint32) = l + r
    static member (-) (l: uint32, r: uint32) = l - r
    static member (*) (l: uint32, r: uint32) = l * r

    static member (+) (l: uint64, r: uint64) = l + r
    static member (-) (l: uint64, r: uint64) = l - r
    static member (*) (l: uint64, r: uint64) = l * r

/// Checked arithmetic operators for Float
type private FloatChecked = 
    static member private Check32 v32 =
        if MIN_FLOAT32 <= float v32 && float v32 <= MAX_FLOAT32 then v32
        else raise (System.OverflowException("Overflow in float32 arithmetic"))
    
    static member private Check64 v64 =
        if MIN_FLOAT64 <= v64 && v64 <= MAX_FLOAT64 then v64
        else raise (System.OverflowException("Overflow in float64 arithmetic"))

    static member (+) (l: float32, r: float32) = l + r |> FloatChecked.Check32
    static member (-) (l: float32, r: float32) = l - r |> FloatChecked.Check32
    static member (*) (l: float32, r: float32) = l * r |> FloatChecked.Check32

    static member (+) (l: float, r: float) = l + r |> FloatChecked.Check64
    static member (-) (l: float, r: float) = l - r |> FloatChecked.Check64
    static member (*) (l: float, r: float) = l * r |> FloatChecked.Check64

// Checked operators are needed for Int and Nat
// Warning unary minus is not checked, maybe a bug in FSharp?
open Microsoft.FSharp.Core.Operators.Checked

type Constant = 

    static member Zero (size: IntType) : Int =
        match size with
        | Int8 -> Int.Zero8 
        | Int16 -> Int.Zero16 
        | Int32 -> Int.Zero32 
        | Int64 -> Int.Zero64

    static member Zero (size: NatType) : Nat =
        match size with
        | Nat8 -> Nat.Zero8
        | Nat16 -> Nat.Zero16
        | Nat32 -> Nat.Zero32
        | Nat64 -> Nat.Zero64

    static member Zero (size: BitsType) : Bits = 
        match size with
        | Bits8 -> Bits.Zero8
        | Bits16 -> Bits.Zero16
        | Bits32 -> Bits.Zero32
        | Bits64 -> Bits.Zero64
    
    static member Zero (size: FloatType) : Float = 
        match size with
        | Float32 -> Float.Zero32
        | Float64 -> Float.Zero64

    static member MinValue (size: IntType) : Int =
        match size with
        | Int8 -> I8 System.SByte.MinValue 
        | Int16 -> I16 System.Int16.MinValue
        | Int32 -> I32 System.Int32.MinValue
        | Int64 -> I64 System.Int64.MinValue

    static member MinValue (size: NatType) : Nat =
        match size with
        | Nat8 -> Nat.Zero8
        | Nat16 -> Nat.Zero16
        | Nat32 -> Nat.Zero32
        | Nat64 -> Nat.Zero64

    static member MinValue (size: BitsType) : Bits = 
        match size with
        | Bits8 -> Bits.Zero8
        | Bits16 -> Bits.Zero16
        | Bits32 -> Bits.Zero32
        | Bits64 -> Bits.Zero64

    static member MinValue (size: FloatType) : Float = 
        match size with
        | Float32 -> F32 System.Single.MinValue
        | Float64 -> F64 System.Double.MinValue

    static member MaxValue (size: IntType) : Int =
        match size with
        | Int8 -> I8 System.SByte.MaxValue 
        | Int16 -> I16 System.Int16.MaxValue
        | Int32 -> I32 System.Int32.MaxValue
        | Int64 -> I64 System.Int64.MaxValue

    static member MaxValue (size: NatType) : Nat =
        match size with
        | Nat8 -> N8 System.Byte.MaxValue 
        | Nat16 -> N16 System.UInt16.MaxValue
        | Nat32 -> N32 System.UInt32.MaxValue
        | Nat64 -> N64 System.UInt64.MaxValue

    static member MaxValue (size: BitsType) : Bits = 
        match size with
        | Bits8 -> B8 System.Byte.MaxValue 
        | Bits16 -> B16 System.UInt16.MaxValue
        | Bits32 -> B32 System.UInt32.MaxValue
        | Bits64 -> B64 System.UInt64.MaxValue

    static member MaxValue (size: FloatType) : Float = 
        match size with
        | Float32 -> F32 System.Single.MaxValue
        | Float64 -> F64 System.Double.MaxValue


type Arithmetic =
    
    // Operator Unm, unary '-'
    
    /// Unary minus operation. 
    /// May throw a System.OverflowException
    static member Unm (i: Int) : Int =
        match i with
        | I8 v -> I8 (0y - v)
        | I16 v -> I16 (0s - v)        
        | I32 v -> I32 (0 - v) 
        | I64 v -> I64 (0L - v)
        | IAny (v, Some s) -> IAny (0I - v, Some <| "-" + s) 
        | IAny (v, None) -> IAny (0I - v, None) 

    static member Unm (bits: Bits) : Bits = 
        match bits with
        | B8 v -> B8 <| BitsUnchecked.(-) (0uy, v)
        | B16 v -> B16 <| BitsUnchecked.(-) (0us, v)        
        | B32 v -> B32 <| BitsUnchecked.(-) (0u, v) 
        | B64 v -> B64 <| BitsUnchecked.(-) (0uL, v)
        | BAny _ -> failwith "Unary Minus for BAny not allowed"
    
    // This will never be called. The type checker guarantees this.
    //static member Unm (nat: Nat) : Nat = 
    //    match nat with
    //    | N8 v -> N8 <| 0uy - v
    //    | N16 v -> N16 <| 0us - v        
    //    | N32 v -> N32 <| 0u - v 
    //    | N64 v -> N64 <| 0uL - v

    static member Unm (f : Float) : Float =
        match f with
        | F32 v -> F32 <| FloatChecked.(-) (0.0f, v) 
        | F64 v -> F64 <| FloatChecked.(-) (0.0, v)
        | FAny (v, Some s) -> FAny (-v, Some <| "-" + s) 
        | FAny (v, None) -> FAny (-v, None) 

    // Operator Add, '+'

    static member Add (left: Int, right: Int) : Int =
        let l = left.PromoteTo right
        let r = right.PromoteTo left
        match l, r with
        | I8 lv, I8 rv -> I8 <| lv + rv 
        | I16 lv, I16 rv -> I16 <| lv + rv 
        | I32 lv, I32 rv -> I32 <| lv + rv 
        | I64 lv, I64 rv -> I64 <| lv + rv 
        | _, _ -> failwith "Add not allowed for IAny or Ints of different size"

    static member Add (left: Bits, right: Bits) : Bits =
        let l = left.PromoteTo right
        let r = right.PromoteTo left
        match l, r with
        | B8 lv, B8 rv -> B8 <| BitsUnchecked.(+) (lv, rv) 
        | B16 lv, B16 rv -> B16 <| BitsUnchecked.(+) (lv, rv) 
        | B32 lv, B32 rv -> B32 <| BitsUnchecked.(+) (lv, rv) 
        | B64 lv, B64 rv -> B64 <| BitsUnchecked.(+) (lv, rv) 
        | _, _ -> failwith "Add not allowed for BAny or Bits of different size"

    static member Add (left: Nat, right: Nat) : Nat =
        let l = left.PromoteTo right
        let r = right.PromoteTo left
        match l, r with
        | N8 lv, N8 rv -> N8 <| lv + rv 
        | N16 lv, N16 rv -> N16 <| lv + rv 
        | N32 lv, N32 rv -> N32 <| lv + rv 
        | N64 lv, N64 rv -> N64 <| lv + rv 
        | _, _ -> failwith "Add not allowed for Nats of different size"

    static member Add (left: Float, right: Float) : Float =
        let l = left.PromoteTo right
        let r = right.PromoteTo left
        match l, r with
        | F32 lv, F32 rv -> F32 <| FloatChecked.(+) (lv, rv)
        | F64 lv, F64 rv -> F64 <| FloatChecked.(+) (lv, rv)
        | _, _ -> failwith "Add not allowed for FAny or Floats of different size"
        
    // Operator Sub, '-'

    static member Sub (left: Int, right: Int) : Int =
        let l = left.PromoteTo right
        let r = right.PromoteTo left
        match l, r with
        | I8 lv, I8 rv -> I8 <| lv - rv 
        | I16 lv, I16 rv -> I16 <| lv - rv 
        | I32 lv, I32 rv -> I32 <| lv - rv 
        | I64 lv, I64 rv -> I64 <| lv - rv 
        | _, _ -> failwith "Sub not allowed for IAny or Int of different size"

    static member Sub (left: Bits, right: Bits) : Bits =
        let l = left.PromoteTo right
        let r = right.PromoteTo left
        match l, r with
        | B8 lv, B8 rv -> B8 <| BitsUnchecked.(-) (lv, rv)
        | B16 lv, B16 rv -> B16 <| BitsUnchecked.(-) (lv, rv) 
        | B32 lv, B32 rv -> B32 <| BitsUnchecked.(-) (lv, rv) 
        | B64 lv, B64 rv -> B64 <| BitsUnchecked.(-) (lv, rv) 
        | _, _ -> failwith "Sub not allowed for BAny or Bits of different size"

    static member Sub (left: Nat, right: Nat) : Nat =
        let l = left.PromoteTo right
        let r = right.PromoteTo left
        match l, r with
        | N8 lv, N8 rv -> N8 <| lv - rv 
        | N16 lv, N16 rv -> N16 <| lv - rv 
        | N32 lv, N32 rv -> N32 <| lv - rv 
        | N64 lv, N64 rv -> N64 <| lv - rv 
        | _, _ -> failwith "Sub not allowed for Nats of different size"

    static member Sub (left: Float, right: Float) : Float =
        let l = left.PromoteTo right
        let r = right.PromoteTo left
        match l, r with
        | F32 lv, F32 rv -> F32 <| FloatChecked.(-) (lv, rv)
        | F64 lv, F64 rv -> F64 <| FloatChecked.(-) (lv, rv)
        | _, _ -> failwith "Sub not allowed for FAny or Floats of different size"
        
    // Operator Mul, '*'

    static member Mul (left: Int, right: Int) : Int =
        let l = left.PromoteTo right
        let r = right.PromoteTo left
        match l, r with
        | I8 lv, I8 rv -> I8 <| lv * rv 
        | I16 lv, I16 rv -> I16 <| lv * rv 
        | I32 lv, I32 rv -> I32 <| lv * rv 
        | I64 lv, I64 rv -> I64 <| lv * rv 
        | _, _ -> failwith "Mul not allowed for IAny or Ints of different size"

    static member Mul (left: Bits, right: Bits) : Bits =
        let l = left.PromoteTo right
        let r = right.PromoteTo left
        match l, r with
        | B8 lv, B8 rv -> B8 <| BitsUnchecked.(*) (lv, rv)
        | B16 lv, B16 rv -> B16 <| BitsUnchecked.(*) (lv, rv)
        | B32 lv, B32 rv -> B32 <| BitsUnchecked.(*) (lv, rv) 
        | B64 lv, B64 rv -> B64 <| BitsUnchecked.(*) (lv, rv) 
        | _, _ -> failwith "Mul not allowed for BAny or Bits of different size"

    static member Mul (left: Nat, right: Nat) : Nat =
        let l = left.PromoteTo right
        let r = right.PromoteTo left
        match l, r with
        | N8 lv, N8 rv -> N8 <| lv * rv 
        | N16 lv, N16 rv -> N16 <| lv * rv 
        | N32 lv, N32 rv -> N32 <| lv * rv 
        | N64 lv, N64 rv -> N64 <| lv * rv 
        | _, _ -> failwith "Mul not allowed for Nats of different size"

    static member Mul (left: Float, right: Float) : Float =
        let l = left.PromoteTo right
        let r = right.PromoteTo left
        match l, r with
        | F32 lv, F32 rv -> F32 <| FloatChecked.(*) (lv, rv)
        | F64 lv, F64 rv -> F64 <| FloatChecked.(*) (lv, rv)
        | _, _ -> failwith "Mul not allowed for FAny or Floats of different size"
        
    // Operator Div, '/'

    static member Div (left: Int, right: Int) : Int =
        let l = left.PromoteTo right
        let r = right.PromoteTo left
        match l, r with
        | I8 lv, I8 rv -> I8 <| lv / rv 
        | I16 lv, I16 rv -> I16 <| lv / rv 
        | I32 lv, I32 rv -> I32 <| lv / rv 
        | I64 lv, I64 rv -> I64 <| lv / rv 
        //| IAny (lv, _), IAny (rv, _) -> IAny (lv / rv, None)
        | _, _ -> failwith "Div not allowed for IAny or Ints of different size"

    static member Div (left: Bits, right: Bits) : Bits =
        let l = left.PromoteTo right
        let r = right.PromoteTo left
        match l, r with
        | B8 lv, B8 rv -> B8 <| lv / rv 
        | B16 lv, B16 rv -> B16 <| lv / rv 
        | B32 lv, B32 rv -> B32 <| lv / rv 
        | B64 lv, B64 rv -> B64 <| lv / rv 
        | _, _ -> failwith "Div not allowed for BAny or Bits of different size"

    static member Div (left: Nat, right: Nat) : Nat =
        let l = left.PromoteTo right
        let r = right.PromoteTo left
        match l, r with
        | N8 lv, N8 rv -> N8 <| lv / rv 
        | N16 lv, N16 rv -> N16 <| lv / rv 
        | N32 lv, N32 rv -> N32 <| lv / rv 
        | N64 lv, N64 rv -> N64 <| lv / rv 
        | _, _ -> failwith "Div not allowed for Nats of different size"

    static member Div (left: Float, right: Float) : Float =
        let l = left.PromoteTo right
        let r = right.PromoteTo left
        match l, r with
        | F32 lv, F32 rv -> F32 <| lv / rv
        | F64 lv, F64 rv -> F64 <| lv / rv
        | _, _ -> failwith "Div not allowed for FAny or Floats of different size"
    
    // Operator Mod, '%', not allowed for Floats

    static member Mod (left: Int, right: Int) : Int =
        let l = left.PromoteTo right
        let r = right.PromoteTo left
        match l, r with
        | I8 lv, I8 rv -> I8 <| lv % rv 
        | I16 lv, I16 rv -> I16 <| lv % rv 
        | I32 lv, I32 rv -> I32 <| lv % rv 
        | I64 lv, I64 rv -> I64 <| lv % rv 
        | _, _ -> failwith "Mod not allowed for IAny or Ints of different size"

    static member Mod (left: Bits, right: Bits) : Bits =
        let l = left.PromoteTo right
        let r = right.PromoteTo left
        match l, r with
        | B8 lv, B8 rv -> B8 <| lv % rv 
        | B16 lv, B16 rv -> B16 <| lv % rv 
        | B32 lv, B32 rv -> B32 <| lv % rv 
        | B64 lv, B64 rv -> B64 <| lv % rv 
        | _, _ -> failwith "Mod not allowed for BAny or Bits of different size"

    static member Mod (left: Nat, right: Nat) : Nat =
        let l = left.PromoteTo right
        let r = right.PromoteTo left
        match l, r with
        | N8 lv, N8 rv -> N8 <| lv % rv 
        | N16 lv, N16 rv -> N16 <| lv % rv 
        | N32 lv, N32 rv -> N32 <| lv % rv 
        | N64 lv, N64 rv -> N64 <| lv % rv 
        | _, _ -> failwith "Mod not allowed for Nats of different size"

// TODO: This is not needed, delete it, fjg. 14.02.20
//type Logical =

//    static member Not (b: bool) : bool = 
//        not b 

//    static member And (left: bool, right: bool) : bool = 
//        left && right

//    static member Or (left: bool, right: bool) : bool = 
//        left || right

type Relational = 
        
    static member Eq (left: Int, right: Int): bool =
        let l = left.PromoteTo right
        let r = right.PromoteTo left    
        match l, r with
        | I8 lv, I8 rv -> lv = rv 
        | I16 lv, I16 rv -> lv = rv 
        | I32 lv, I32 rv -> lv = rv 
        | I64 lv, I64 rv -> lv = rv
        | IAny (lv, _), IAny (rv, _) -> lv = rv
        | _, _ -> failwith "Invalid Eq for Int"  
    
    static member Eq (left: Nat, right: Nat): bool =
        let l = left.PromoteTo right
        let r = right.PromoteTo left    
        match l, r with
        | N8 lv, N8 rv -> lv = rv 
        | N16 lv, N16 rv -> lv = rv 
        | N32 lv, N32 rv -> lv = rv 
        | N64 lv, N64 rv -> lv = rv
        | _, _ -> failwith "Invalid Eq for Nat"  
        
    static member Eq (left: Bits, right: Bits): bool =
        let l = left.PromoteTo right
        let r = right.PromoteTo left    
        match l, r with
        | B8 lv, B8 rv -> lv = rv 
        | B16 lv, B16 rv -> lv = rv 
        | B32 lv, B32 rv -> lv = rv 
        | B64 lv, B64 rv -> lv = rv
        | BAny (lv, _), BAny (rv, _) -> lv = rv
        | _, _ -> failwith "Invalid Eq for Bits"  

    static member Eq (left: Float, right: Float): bool =
        let l = left.PromoteTo right
        let r = right.PromoteTo left    
        match l, r with
        | F32 lv, F32 rv -> lv = rv
        | F64 lv, F64 rv -> lv = rv
        | FAny (lv, _), FAny (rv, _) -> lv = rv
        | _, _ -> failwith "Invalid Eq for Float"  


    static member Lt (left: Int, right: Int): bool =
        let l = left.PromoteTo right
        let r = right.PromoteTo left    
        match l, r with
        | I8 lv, I8 rv -> lv < rv 
        | I16 lv, I16 rv -> lv < rv 
        | I32 lv, I32 rv -> lv < rv 
        | I64 lv, I64 rv -> lv < rv
        | IAny (lv, _), IAny (rv, _) -> lv < rv
        | _, _ -> failwith "Invalid Lt for Int"  

    static member Lt (left: Nat, right: Nat): bool =
        let l = left.PromoteTo right
        let r = right.PromoteTo left    
        match l, r with
        | N8 lv, N8 rv -> lv < rv 
        | N16 lv, N16 rv -> lv < rv 
        | N32 lv, N32 rv -> lv < rv 
        | N64 lv, N64 rv -> lv < rv
        | _, _ -> failwith "Invalid Lt for Nat"  
    
    static member Lt (left: Bits, right: Bits): bool =
        let l = left.PromoteTo right
        let r = right.PromoteTo left    
        match l, r with
        | B8 lv, B8 rv -> lv < rv 
        | B16 lv, B16 rv -> lv < rv 
        | B32 lv, B32 rv -> lv < rv 
        | B64 lv, B64 rv -> lv < rv
        | BAny (lv, _), BAny (rv, _) -> lv < rv
        | _, _ -> failwith "Invalid Lt for Bits"  

    static member Lt (left: Float, right: Float): bool =
        let l = left.PromoteTo right
        let r = right.PromoteTo left    
        match l, r with
        | F32 lv, F32 rv -> lv < rv
        | F64 lv, F64 rv -> lv < rv
        | FAny (lv, _), FAny (rv, _) -> lv < rv
        | _, _ -> failwith "Invalid Lt for Float"  

    static member Le (left: Int, right: Int): bool =
        let l = left.PromoteTo right
        let r = right.PromoteTo left    
        match l, r with
        | I8 lv, I8 rv -> lv <= rv 
        | I16 lv, I16 rv -> lv <= rv 
        | I32 lv, I32 rv -> lv <= rv 
        | I64 lv, I64 rv -> lv <= rv
        | IAny (lv, _), IAny (rv, _) -> lv <= rv
        | _, _ -> failwith "Invalid Le for Int"  

    static member Le (left: Nat, right: Nat): bool =
        let l = left.PromoteTo right
        let r = right.PromoteTo left    
        match l, r with
        | N8 lv, N8 rv -> lv <= rv 
        | N16 lv, N16 rv -> lv <= rv 
        | N32 lv, N32 rv -> lv <= rv 
        | N64 lv, N64 rv -> lv <= rv
        | _, _ -> failwith "Invalid Le for Nat"  

    static member Le (left: Bits, right: Bits): bool =
        let l = left.PromoteTo right
        let r = right.PromoteTo left    
        match l, r with
        | B8 lv, B8 rv -> lv <= rv 
        | B16 lv, B16 rv -> lv <= rv 
        | B32 lv, B32 rv -> lv <= rv 
        | B64 lv, B64 rv -> lv <= rv
        | BAny (lv, _), BAny (rv, _) -> lv <= rv
        | _, _ -> failwith "Invalid Le for Bits"  

    static member Le (left: Float, right: Float): bool =
        let l = left.PromoteTo right
        let r = right.PromoteTo left    
        match l, r with
        | F32 lv, F32 rv -> lv <= rv
        | F64 lv, F64 rv -> lv <= rv
        | FAny (lv, _), FAny (rv, _) -> lv <= rv
        | _, _ -> failwith "Invalid Le for Float"  



and Bitwise =  

    static member Bnot (bits: Bits) =
        match bits with
        | B8 b -> B8 ~~~b        
        | B16 b -> B16 ~~~b        
        | B32 b -> B32 ~~~b        
        | B64 b -> B64 ~~~b        
        | _ -> failwith "Bnot on AnyBits not allowed"
    
    static member Band (left: Bits, right: Bits) =
        let l = left.PromoteTo right
        let r = right.PromoteTo left
        match l, r with
        | B8 lv, B8 rv -> B8 <| (lv &&& rv) 
        | B16 lv, B16 rv -> B16 <| (lv &&& rv) 
        | B32 lv, B32 rv -> B32 <| (lv &&& rv)
        | B64 lv, B64 rv -> B64 <| (lv &&& rv) 
        | _ -> failwith "No BAnd on BAny allowed"
    
    static member Bor (left: Bits, right: Bits) =
        let l = left.PromoteTo right
        let r = right.PromoteTo left
        match l, r with
        | B8 lv, B8 rv -> B8 <| (lv ||| rv) 
        | B16 lv, B16 rv -> B16 <| (lv ||| rv) 
        | B32 lv, B32 rv -> B32 <| (lv ||| rv)
        | B64 lv, B64 rv -> B64 <| (lv ||| rv) 
        | _ -> failwith "No BOr on BAny allowed"
    
    static member Bxor (left: Bits, right: Bits) =
        let l = left.PromoteTo right
        let r = right.PromoteTo left
        match l, r with
        | B8 lv, B8 rv -> B8 <| (lv ^^^ rv) 
        | B16 lv, B16 rv -> B16 <| (lv ^^^ rv) 
        | B32 lv, B32 rv -> B32 <| (lv ^^^ rv)
        | B64 lv, B64 rv -> B64 <| (lv ^^^ rv) 
        | _ -> failwith "No BXor on BAny allowed"
    
    static member Shl (bits: Bits, amount: int32) : Bits =
        match bits with
        | B8 b -> B8 (b <<< amount)
        | B16 b -> B16 (b <<< amount)
        | B32 b -> B32 (b <<< amount)
        | B64 b -> B64 (b <<< amount)
        | BAny _ -> failwith "No Shl on BAny allowed"

    static member Shr (bits: Bits, amount: int32) : Bits =
        match bits with
        | B8 b -> B8 (b >>> amount)
        | B16 b -> B16 (b >>> amount)
        | B32 b -> B32 (b >>> amount)
        | B64 b -> B64 (b >>> amount)
        | BAny _ -> failwith "No Shl on BAny allowed"

    static member Sshr (bits: Bits, amount: int32) : Bits =
        match bits with
        | B8 b -> int8 b >>> amount |> uint8 |> B8
        | B16 b -> int16 b >>> amount |> uint16 |> B16
        | B32 b -> int32 b >>> amount |> uint32 |> B32
        | B64 b -> int64 b >>> amount |> uint64 |> B64
        | BAny _ -> failwith "No signed shift right '+>>' on BAny allowed"

    static member Rotl (bits: Bits, amount: int32) : Bits =
        match bits with
        | B8 b -> b <<< amount ||| b >>> 8 - amount |> B8 
        | B16 b -> b <<< amount ||| b >>> 16 - amount |> B16 
        | B32 b -> b <<< amount ||| b >>> 32 - amount |> B32 
        | B64 b -> b <<< amount ||| b >>> 64 - amount |> B64 
        | BAny _ -> failwith "No rotate left '<<>' on BAny allowed"

    static member Rotr (bits: Bits, amount: int32) : Bits =
        match bits with
        | B8 b -> b >>> amount ||| b <<< 8 - amount |> B8 
        | B16 b -> b >>> amount ||| b <<< 16 - amount |> B16 
        | B32 b -> b >>> amount ||| b <<< 32 - amount |> B32 
        | B64 b -> b >>> amount ||| b <<< 64 - amount |> B64 
        | BAny _ -> failwith "No rotate right '<>>' on BAny allowed"


module Widen = 
    
    let IntToInt (i: Int, it: IntType) : Int =
        match i, it with
        | I8 _, Int8 -> i
        | I8 i, Int16 -> I16 (int16 i)
        | I16 _, Int16 -> i
        | I8 i, Int32 -> I32 (int32 i)
        | I16 i, Int32 -> I32 (int32 i)
        | I32 _, Int32 -> i
        | I8 i, Int64 -> I64 (int64 i)
        | I16 i, Int64 -> I64 (int64 i)
        | I32 i, Int64 -> I64 (int64 i)
        | I64 _, Int64 -> i
        | _ -> failwith (sprintf "No conversion from %A to %A" i it)

    let IntToNat (i: Int, nt: NatType) : Nat =
        match i, nt with
        | I8 i, Nat8 -> N8 (uint8 i)
        | I8 i, Nat16 -> N16 (uint16 i)
        | I16 i, Nat16 -> N16 (uint16 i)
        | I8 i, Nat32 -> N32 (uint32 i)
        | I16 i, Nat32 -> N32 (uint32 i)
        | I32 i, Nat32 -> N32 (uint32 i)
        | I8 i, Nat64 -> N64 (uint64 i)
        | I16 i, Nat64 -> N64 (uint64 i)
        | I32 i, Nat64 -> N64 (uint64 i)
        | I64 i, Nat64 -> N64 (uint64 i)
        | _ -> failwith (sprintf "No conversion from %A to %A" i nt)

    let IntToBits (i: Int, bt: BitsType) : Bits =
        match i, bt with
        | I8 i, Bits8 -> B8 (uint8 i)
        | I8 i, Bits16 -> B16 (uint16 i)
        | I16 i, Bits16 -> B16 (uint16 i)
        | I8 i, Bits32 -> B32 (uint32 i)
        | I16 i, Bits32 -> B32 (uint32 i)
        | I32 i, Bits32 -> B32 (uint32 i)
        | I8 i, Bits64 -> B64 (uint64 i)
        | I16 i, Bits64 -> B64 (uint64 i)
        | I32 i, Bits64 -> B64 (uint64 i)
        | I64 i, Bits64 -> B64 (uint64 i)
        | _ -> failwith (sprintf "No conversion from %A to %A" i bt)

    let IntToFloat (i: Int, ft: FloatType) : Float =
        match i, ft with
        | I8 i, Float32 -> F32 (float32 i)
        | I16 i, Float32 -> F32 (float32 i)
        | I8 i, Float64 -> F64 (float i)
        | I16 i, Float64 -> F64 (float i)
        | I32 i, Float64 -> F64 (float i)
        | _ -> failwith (sprintf "No conversion from %A to %A" i ft)

    let NatToNat (n: Nat, nt: NatType) : Nat =
        match n, nt with
        | N8 _, Nat8 -> n
        | N8 n, Nat16 -> N16 (uint16 n)
        | N16 _, Nat16 -> n
        | N8 n, Nat32 -> N32 (uint32 n)
        | N16 n, Nat32 -> N32 (uint32 n)
        | N32 _, Nat32 -> n
        | N8 n, Nat64 -> N64 (uint64 n)
        | N16 n, Nat64 -> N64 (uint64 n)
        | N32 n, Nat64 -> N64 (uint64 n)
        | N64 _, Nat64 -> n
        | _ -> failwith (sprintf "No conversion from %A to %A" n nt)

    let NatToBits (n: Nat, bt: BitsType) : Bits =
        match n, bt with
        | N8 n, Bits8 -> B8 (uint8 n)
        | N8 n, Bits16 -> B16 (uint16 n)
        | N16 n, Bits16 -> B16 (uint16 n)
        | N8 n, Bits32 -> B32 (uint32 n)
        | N16 n, Bits32 -> B32 (uint32 n)
        | N32 n, Bits32 -> B32 (uint32 n)
        | N8 n, Bits64 -> B64 (uint64 n)
        | N16 n, Bits64 -> B64 (uint64 n)
        | N32 n, Bits64 -> B64 (uint64 n)
        | N64 n, Bits64 -> B64 (uint64 n)
        | _ -> failwith (sprintf "No conversion from %A to %A" n bt)

    let NatToInt (n: Nat, it: IntType) : Int =
        match n, it with
        | N8 n, Int16 -> I16 (int16 n)
        | N8 n, Int32 -> I32 (int32 n)
        | N16 n, Int32 -> I32 (int32 n)
        | N8 n, Int64 -> I64 (int64 n)
        | N16 n, Int64 -> I64 (int64 n)
        | N32 n, Int64 -> I64 (int64 n)
        | _ -> failwith (sprintf "No conversion from %A to %A" n it)

    let NatToFloat (i: Nat, ft: FloatType) : Float =
        match i, ft with
        | N8 n, Float32 -> F32 (float32 n)
        | N16 n, Float32 -> F32 (float32 n)
        | N8 n, Float64 -> F64 (float n)
        | N16 n, Float64 -> F64 (float n)
        | N32 n, Float64 -> F64 (float n)
        | _ -> failwith (sprintf "No conversion from %A to %A" i ft)

    let BitsToBits (b: Bits, bt: BitsType) : Bits =
        match b, bt with
        | B8 _, Bits8 -> b
        | B8 b, Bits16 -> B16 (uint16 b)
        | B16 _, Bits16 -> b
        | B8 b, Bits32 -> B32 (uint32 b)
        | B16 b, Bits32 -> B32 (uint32 b)
        | B32 _, Bits32 -> b
        | B8 b, Bits64 -> B64 (uint64 b)
        | B16 b, Bits64 -> B64 (uint64 b)
        | B32 b, Bits64 -> B64 (uint64 b)
        | B64 _, Bits64 -> b
        | _ -> failwith (sprintf "No conversion from %A to %A" b bt)

    let BitsToNat (b: Bits, nt: NatType) : Nat =
        match b, nt with
        | B8 b, Nat8 -> N8 (uint8 b)
        | B8 b, Nat16 -> N16 (uint16 b)
        | B16 b, Nat16 -> N16 (uint16 b)
        | B8 b, Nat32 -> N32 (uint32 b)
        | B16 b, Nat32 -> N32 (uint32 b)
        | B32 b, Nat32 -> N32 (uint32 b)
        | B8 n, Nat64 -> N64 (uint64 b)
        | B16 b, Nat64 -> N64 (uint64 b)
        | B32 b, Nat64 -> N64 (uint64 b)
        | B64 b, Nat64 -> N64 (uint64 b)
        | _ -> failwith (sprintf "No conversion from %A to %A" b nt)

    let BitsToInt (b: Bits, it: IntType) : Int =
        match b, it with
        | B8 b, Int16 -> I16 (int16 b)
        | B8 b, Int32 -> I32 (int32 b)
        | B16 b, Int32 -> I32 (int32 b)
        | B8 b, Int64 -> I64 (int64 b)
        | B16 b, Int64 -> I64 (int64 b)
        | B32 b, Int64 -> I64 (int64 b)
        | _ -> failwith (sprintf "No conversion from %A to %A" b it)

    let BitsToFloat (b: Bits, ft: FloatType) : Float =
        match b, ft with
        | B8 b, Float32 -> F32 (float32 b)
        | B16 b, Float32 -> F32 (float32 b)
        | B8 b, Float64 -> F64 (float b)
        | B16 b, Float64 -> F64 (float b)
        | B32 b, Float64 -> F64 (float b)
        | _ -> failwith (sprintf "No conversion from %A to %A" b ft)

    let FloatToFloat (f: Float, ft: FloatType) : Float =
        match f, ft with
        | F32 _, Float32 -> f
        | F32 f, Float64 -> F64 (float f)
        | F64 _, Float64 -> f
        | _ -> failwith (sprintf "No conversion from %A to %A" f ft)


module Narrow = 

    let IntToInt (i: Int, it: IntType) : Int =
        match i, it with
        | I8 _, Int8 -> i
        | I16 i, Int8 -> I8 <| int8 i
        | I32 i, Int8 -> I8 <| int8 i
        | I64 i, Int8 -> I8 <| int8 i
        | I16 _, Int16 -> i
        | I32 i, Int16 -> I16 <| int16 i
        | I64 i, Int16 -> I16 <| int16 i
        | I32 _, Int32 -> i
        | I64 i, Int32 -> I32 <| int32 i
        | I64 _, Int64 -> i
        | _ -> failwith (sprintf "No conversion from %A to %A" i it)

    let IntToNat (i: Int, nt: NatType) : Nat =
        match i, nt with
        | I8 i, Nat8 -> N8 <| uint8 i
        | I16 i, Nat8 -> N8 <| uint8 i
        | I32 i, Nat8 -> N8 <| uint8 i
        | I64 i, Nat8 -> N8 <| uint8 i
        | I16 i, Nat16 -> N16 <| uint16 i
        | I32 i, Nat16 -> N16 <| uint16 i
        | I64 i, Nat16 -> N16 <| uint16 i
        | I32 i, Nat32 -> N32 <| uint32 i
        | I64 i, Nat32 -> N32 <| uint32 i
        | I64 i, Nat64 -> N64 <| uint64 i
        | _ -> failwith (sprintf "No conversion from %A to %A" i nt)

    let IntToBits (i: Int, bt: BitsType) : Bits =
        match i, bt with
        | I8 i, Bits8 -> B8 <| uint8 i
        | I16 i, Bits8 -> B8 <| uint8 i
        | I32 i, Bits8 -> B8 <| uint8 i
        | I64 i, Bits8 -> B8 <| uint8 i
        | I16 i, Bits16 -> B16 <| uint16 i
        | I32 i, Bits16 -> B16 <| uint16 i
        | I64 i, Bits16 -> B16 <| uint16 i
        | I32 i, Bits32 -> B32 <| uint32 i
        | I64 i, Bits32 -> B32 <| uint32 i
        | I64 i, Bits64 -> B64 <| uint64 i
        | _ -> failwith (sprintf "No conversion from %A to %A" i bt)

    let IntToFloat (i: Int, ft: FloatType) : Float =
        match i, ft with
        | I32 i, Float32 -> F32 <| float32 i
        | I64 i, Float32 -> F32 <| float32 i
        | I64 i, Float64 -> F64 <| float i
        | _ -> failwith (sprintf "No conversion from %A to %A" i ft)

    let NatToNat (n: Nat, nt: NatType) : Nat =
        match n, nt with
        | N8 _, Nat8 -> n
        | N16 n, Nat8 -> N8 <| uint8 n
        | N32 n, Nat8 -> N8 <| uint8 n
        | N64 n, Nat8 -> N8 <| uint8 n
        | N16 _, Nat16 -> n
        | N32 n, Nat16 -> N16 <| uint16 n
        | N64 n, Nat16 -> N16 <| uint16 n
        | N32 _, Nat32 -> n
        | N64 n, Nat32 -> N32 <| uint32 n
        | N64 _, Nat64 -> n
        | _ -> failwith (sprintf "No conversion from %A to %A" n nt)

    let NatToBits (n: Nat, bt: BitsType) : Bits =
        match n, bt with
        | N8 n, Bits8 -> B8 <| uint8 n
        | N16 n, Bits8 -> B8 <| uint8 n
        | N32 n, Bits8 -> B8 <| uint8 n
        | N64 n, Bits8 -> B8 <| uint8 n
        | N16 n, Bits16 -> B16 <| uint16 n
        | N32 n, Bits16 -> B16 <| uint16 n
        | N64 n, Bits16 -> B16 <| uint16 n
        | N32 n, Bits32 -> B32 <| uint32 n
        | N64 n, Bits32 -> B32 <| uint32 n
        | N64 n, Bits64 -> B64 <| uint64 n
        | _ -> failwith (sprintf "No conversion from %A to %A" n bt)

    let NatToInt (n: Nat, it: IntType) : Int =
        match n, it with
        | N8 n, Int8 -> I8 <| int8 n
        | N16 n, Int8 -> I8 <| int8 n
        | N32 n, Int8 -> I8 <| int8 n
        | N64 n, Int8 -> I8 <| int8 n
        | N16 n, Int16 -> I16 <| int16 n
        | N32 n, Int16 -> I16 <| int16 n
        | N64 n, Int16 -> I16 <| int16 n
        | N32 n, Int32 -> I32 <| int32 n
        | N64 n, Int32 -> I32 <| int32 n
        | N64 n, Int64 -> I64 <| int64 n
        | _ -> failwith (sprintf "No conversion from %A to %A" n it)

    let NatToFloat (n: Nat, ft: FloatType) : Float =
        match n, ft with
        | N32 n, Float32 -> F32 <| float32 n
        | N64 n, Float32 -> F32 <| float32 n
        | N64 n, Float64 -> F64 <| float n
        | _ -> failwith (sprintf "No conversion from %A to %A" n ft)

    let BitsToBits (b: Bits, bt: BitsType) : Bits =
        match b, bt with
        | B8 _, Bits8 -> b
        | B16 b, Bits8 -> B8 <| uint8 b
        | B32 b, Bits8 -> B8 <| uint8 b
        | B64 b, Bits8 -> B8 <| uint8 b
        | B16 _, Bits16 -> b
        | B32 b, Bits16 -> B16 <| uint16 b
        | B64 b, Bits16 -> B16 <| uint16 b
        | B32 _, Bits32 -> b
        | B64 b, Bits32 -> B32 <| uint32 b
        | B64 _, Bits64 -> b
        | _ -> failwith (sprintf "No conversion from %A to %A" b bt)

    let BitsToNat (b: Bits, nt: NatType) : Nat =
        match b, nt with
        | B8 b, Nat8 -> N8 <| uint8 b
        | B16 b, Nat8 -> N8 <| uint8 b
        | B32 b, Nat8 -> N8 <| uint8 b
        | B64 b, Nat8 -> N8 <| uint8 b
        | B16 b, Nat16 -> N16 <| uint16 b
        | B32 b, Nat16 -> N16 <| uint16 b
        | B64 b, Nat16 -> N16 <| uint16 b
        | B32 b, Nat32 -> N32 <| uint32 b
        | B64 b, Nat32 -> N32 <| uint32 b
        | B64 b, Nat64 -> N64 <| uint64 b
        | _ -> failwith (sprintf "No conversion from %A to %A" b nt)

    let BitsToInt (b: Bits, it: IntType) : Int =
        match b, it with
        | B8 b, Int8 -> I8 <| int8 b
        | B16 b, Int8 -> I8 <| int8 b
        | B32 b, Int8 -> I8 <| int8 b
        | B64 b, Int8 -> I8 <| int8 b
        | B16 b, Int16 -> I16 <| int16 b
        | B32 b, Int16 -> I16 <| int16 b
        | B64 b, Int16 -> I16 <| int16 b
        | B32 b, Int32 -> I32 <| int32 b
        | B64 b, Int32 -> I32 <| int32 b
        | B64 b, Int64 -> I64 <| int64 b
        | _ -> failwith (sprintf "No conversion from %A to %A" b it)

    let BitsToFloat (b: Bits, ft: FloatType) : Float =
        match b, ft with
        | B32 b, Float32 -> F32 <| float32 b
        | B64 b, Float32 -> F32 <| float32 b
        | B64 b, Float64 -> F64 <| float b
        | _ -> failwith (sprintf "No conversion from %A to %A" b ft)

    let FloatToFloat (f: Float, ft: FloatType) : Float =
        match f, ft with
        | F32 _, Float32 -> f
        | F64 f, Float32 -> F64 <| float f
        | F64 _, Float64 -> f
        | _ -> failwith (sprintf "No conversion from %A to %A" f ft)

    let FloatToInt (f: Float, it: IntType) : Int =
        match f, it with
        | F32 f, Int8 -> I8 <| int8 f
        | F32 f, Int16 -> I16 <| int16 f
        | F32 f, Int32 -> I32 <| int32 f
        | F32 f, Int64 -> I64 <| int64 f
        | F64 f, Int8 -> I8 <| int8 f
        | F64 f, Int16 -> I16 <| int16 f
        | F64 f, Int32 -> I32 <| int32 f
        | F64 f, Int64 -> I64 <| int64 f
        | _ -> failwith (sprintf "No conversion from %A to %A" f it)

    let FloatToNat (f: Float, nt: NatType) : Nat =
        match f, nt with
        | F32 f, Nat8 -> N8 <| uint8 f
        | F32 f, Nat16 -> N16 <| uint16 f
        | F32 f, Nat32 -> N32 <| uint32 f
        | F32 f, Nat64 -> N64 <| uint64 f
        | F64 f, Nat8 -> N8 <| uint8 f
        | F64 f, Nat16 -> N16 <| uint16 f
        | F64 f, Nat32 -> N32 <| uint32 f
        | F64 f, Nat64 -> N64 <| uint64 f
        | _ -> failwith (sprintf "No conversion from %A to %A" f nt)

    let FloatToBits (f: Float, bt: BitsType) : Bits =
        match f, bt with
        | F32 f, Bits8 -> B8 <| uint8 f
        | F32 f, Bits16 -> B16 <| uint16 f
        | F32 f, Bits32 -> B32 <| uint32 f
        | F32 f, Bits64 -> B64 <| uint64 f
        | F64 f, Bits8 -> B8 <| uint8 f
        | F64 f, Bits16 -> B16 <| uint16 f
        | F64 f, Bits32 -> B32 <| uint32 f
        | F64 f, Bits64 -> B64 <| uint64 f
        | _ -> failwith (sprintf "No conversion from %A to %A" f bt)

    // TODO: add remaining float narrowing