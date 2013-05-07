using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class Int2 : System.Object{
	public int x;
	public int y;
	public Int2(int x, int y){
		this.x = x;
		this.y = y;
	}
	
	//Checks if two tiles are adjacent
	public bool isNextTo(Int2 b){
		return 	(x-b.x)*(x-b.x) <= 1 &&
				(y-b.y)*(y-b.y) <= 1;
	}
	
	#region operatorOverloads
	//Subtraction
	public static Int2 operator-(Int2 a, Int2 b){
		return new Int2(a.x - b.x, a.y - b.y);
	}
	
	//Addition
	public static Int2 operator+(Int2 a, Int2 b){
		return new Int2(a.x + b.x, a.y + b.y);
	}
	
	//Equality
	public static bool operator==(Int2 a, Int2 b){
		if (System.Object.ReferenceEquals(a, b))
			return true;
		if((object)a == null || (object)b==null)
			return false;
		return a.x == b.x && a.y == b.y;
	}
	
	public override bool Equals (object obj)
	{
		if (obj == null)
			return false;
		
		Int2 i2 = obj as Int2;
		if ((System.Object)i2 == null)
			return false;
		return base.Equals (obj);
	}
	
	//Inequality ("back of the bus woman!")
	public static bool operator!=(Int2 a, Int2 b){
		return !(a==b);
	}
	
	public override int GetHashCode ()
	{
		return x^y;
	}
	#endregion operatorOverloads
}

public struct Tile{
	public TileState	state;
	public float		height;
	public int			pathingCost;
	public Tile(TileState state, float height, int pathingCost){
		this.state  = state;
		this.height = height;
		this.pathingCost = pathingCost;
	}
}

public enum TileState{
	PASSABLE,
	IMPASSABLE
}

public enum TerrainGenType{
	RANDOM,
	DIAMOND_SQUARE
}

public struct Node : IComparable<Node>{
	public Int2 tile;
	public Int2 prevTile;
	public int costSoFar;
	public int estimatedTotalCost;
	
	public Node(int x, int y){
		tile = new Int2(x,y);
		prevTile = new Int2(0,0);
		costSoFar = 0;
		estimatedTotalCost = 0;
	}
	public Node(Int2 tile){
		this.tile = tile;
		prevTile = new Int2(0,0);
		costSoFar = 0;
		estimatedTotalCost = 0;
	}
	
	public int CompareTo(Node other){
		if ((System.Object)other == null)return 1;
		if (other.tile == this.tile){
			return 0;
		}else{
			return tile.GetHashCode() > other.tile.GetHashCode() ? 1 : -1;
		}
	}
}

//Encapsulates a predicate to take a Int2 to match against a nodes tile
public class matchesTile{
	private Int2 m_baseTile;
	public matchesTile(Int2 baseTile){
		m_baseTile = baseTile;
	}
	
	//sets a different base tile
	public Int2 BaseTile{
		get{return m_baseTile;}
		set{m_baseTile = value;}
	}
	
	//gets the predicate
	public Predicate<Node> Match{
		get{return IsMatch;}
	}
	
	//The actual predicate
	private bool IsMatch(Node n){
		return m_baseTile == n.tile;
	}
}

public struct Connection{
	public Int2 fromTile;
	public Int2 toTile;
	public int cost;	
}

public class byTile : IComparer<Node>{
	public int Compare(Node a, Node b){
		if (a.tile == b.tile){
			return 0;
		}else{
			return a.tile.GetHashCode() > b.tile.GetHashCode() ? 1 : -1;
		}
	}
}

public class byEstimatedTotalCost : IComparer<Node>{
	public int Compare(Node a, Node b){
		if(a.estimatedTotalCost < b.estimatedTotalCost){
			return -1;
		}else if (a.estimatedTotalCost == b.estimatedTotalCost){
			return 0;
		}else{
			return 1;
		}
	}
}

public delegate float tileCostDelegate(Int2 current, Int2 destination);

public enum godMode{
	PATHS,
	PASSABLE,
	IMPASSABLE,
	FOOD,
	ANT
}