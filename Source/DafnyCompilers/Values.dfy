include "CompilerCommon.dfy"
include "Library.dfy"

module Values {
  import Lib.Math
  import opened Lib.Datatypes
  import DafnyCompilerCommon.AST.Types

  datatype Value =
    | Bool(b: bool)
    | Char(c: char)
    | Int(i: int)
    | Real(r: real)
    | BigOrdinal(o: ORDINAL)
    | BitVector(value: int)
    | Map(m: map<Value, Value>)
    | MultiSet(ms: multiset<Value>)
    | Seq(sq: seq<Value>)
    | Set(st: set<Value>)
  {
    predicate method HasType(ty: Types.T) {
      match (this, ty) // FIXME tests on other side
        case (Bool(b), Bool()) => true
        case (Char(c), Char()) => true
        case (Int(i), Int()) => true
        case (Real(r), Real()) => true
        case (BigOrdinal(o), BigOrdinal()) => true
        case (BitVector(value), BitVector(width)) =>
          0 <= value < Math.IntPow(2, width)
        case (Map(m), Collection(true, Map(kT), eT)) =>
          forall x | x in m :: x.HasType(kT) && m[x].HasType(eT)
        case (MultiSet(ms), Collection(true, MultiSet, eT)) =>
          forall x | x in ms :: x.HasType(eT)
        case (Seq(sq), Collection(true, Seq, eT)) =>
          forall x | x in sq :: x.HasType(eT)
        case (Set(st), Collection(true, Set, eT)) =>
          forall x | x in st :: x.HasType(eT)

        case (Bool(b), _) => false
        case (Char(c), _) => false
        case (Int(i), _) => false
        case (Real(r), _) => false
        case (BigOrdinal(o), _) => false
        case (BitVector(value), _) => false
        case (Map(m), _) => false
        case (MultiSet(ms), _) => false
        case (Seq(sq), _) => false
        case (Set(st), _) => false
    }

    function method Cast(ty: Types.T) : (v: Option<Value>)
      ensures v.Some? ==> HasType(ty)
    {
      if HasType(ty) then Some(this) else None
    }
  }

  type T = Value

  datatype TypedValue = Value(ty: Types.T, v: T) {
    predicate Wf() { v.HasType(ty) }
  }

  // FIXME how do we define the interpreter to support :| without committing to a single interpretation of :|?
  // NOTE: Maybe tag each syntactic :| with a distinct color and add the color somehow into the Dafny-side :| used to implement it.  Pro: it allows inlining.
}

type Value = Values.T
