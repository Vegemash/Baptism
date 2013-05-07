/*
 * Author: 	Isaac Jackson
 * Email:	MrIsaacJackson@gmail.com
 * Date:	6/2/2012
 * This is just a muck-around script that draws a 
 * grid onto a texture and allows some pathing
 * */
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class SquareWalks : MonoBehaviour {
	//Texture
	public Texture2D 	tex;
	//Terrain type
	public TerrainGenType terrainType = TerrainGenType.DIAMOND_SQUARE;
	//Tile colors
	public Color passCol = new Color(0,0.5f,0,1);
	public Color impassCol = new Color(0,0.3f,0,1);
	public List<Tile>	tiles;
	//Grid dimensions = 2^size = 2^(size*2) tiles
	public const int	size = 7;
	//grid dimensions
	private int 	width;
	private int 	height;
	//Square root of pixels per tile
	public const int	tileScale = 1;
	//Time between drawing tiles of the path
	public float pathDrawStep = 0.125f;
	//Time between setting random paths
	public float randomPathInterval =  10.0f;
	//Turns on and off random pathing
	private bool bDrawRandomPaths = false;
	//Controls what clicking does
	private godMode mode = godMode.PATHS;
	//Path selection vars
	bool firstClick = true;
	Int2 initTile;
	Int2 destTile;
	// Use this for initialization
	void Start () {
		//This shit is why you need an exponent
		height = 2;
		for(int exp = size; exp > 1; exp--){
			height *= 2;
		}
		width = height;
		initTile = new Int2(0,0);
		destTile = new Int2(width/2, height/2);
		tex = new Texture2D(width*tileScale, height*tileScale);
		List<Color32> colors =  new List<Color32>();
		tiles = new List<Tile>();
		System.Random rand = new System.Random();
		//Fill image with white
		for(int ii = 0; ii < width*height*tileScale*tileScale; ii++)
			colors.Add(Color.white);
		tex.SetPixels32(colors.ToArray());
		tex.filterMode = FilterMode.Point;
		//Create terrain
		CreateTerrain (rand, terrainType);
		//Draw terrain
		clearImage();
		tex.Apply();
		renderer.material.SetTexture("_MainTex",tex);
	}
	
	// Update is called once per frame
	void Update () {
		if(Input.GetMouseButtonDown(0)){
			RaycastHit hit;
			if(gameObject.collider.Raycast(//Raycast
				Camera.main.ScreenPointToRay(Input.mousePosition)//Along ray from screen to mouse position
				,out hit,1000)){
				handleClicks(hit);
			}
		}
		if (Input.GetKeyDown(KeyCode.Return)){
			clearImage();
		}
		if(Input.GetKeyDown(KeyCode.Z))
			showZeroHeightTiles();
		if(Input.GetKeyDown(KeyCode.R))
			redoTerrain();
		if(Input.GetKeyDown(KeyCode.P)){
			if(!bDrawRandomPaths){
				bDrawRandomPaths = true;
				StartCoroutine(drawRandomPaths());
			}else{
				bDrawRandomPaths = false;
			}
		}
		if(Input.GetKeyDown(KeyCode.A))
			mode = godMode.PATHS;
		if(Input.GetKeyDown(KeyCode.O))
			mode = godMode.PASSABLE;
		if(Input.GetKeyDown(KeyCode.E))
			mode = godMode.IMPASSABLE;
		
		if(Input.GetKeyDown(KeyCode.C))
			drawPathingCosts();			
	}
	
	//Handles clicks and sets endpoints for paths
	void handleClicks (RaycastHit hit)
	{
		Vector2 uvHit = hit.textureCoord;
		Int2 hitPos = new Int2((int)(width * uvHit.x), (int)(height * uvHit.y));
		switch(mode){
		case godMode.PATHS:
			if (isValidTile(hitPos)) {
				drawTile(hitPos, Color.red, true);
				//print("x:" + hitPos.x + ", y:" + hitPos.y +
				//	", u:" + uvHit.x + ", v:" + uvHit.y);
				//Select the enpoints of the path
				if(firstClick){
					initTile = hitPos;
					firstClick = false;
				}else{
					destTile = hitPos;
					firstClick = true;
					StartCoroutine(AStar(initTile, destTile));
				}
			}
			break;
		case godMode.PASSABLE:
			Tile temp = tiles[tileIndex(hitPos)];
			temp.state = TileState.PASSABLE;
			tiles[tileIndex(hitPos)] = temp;
			drawTile(hitPos, passCol, true);
			break;
		case godMode.IMPASSABLE:
			Tile temp1 = tiles[tileIndex(hitPos)];
			temp1.state = TileState.IMPASSABLE;
			tiles[tileIndex(hitPos)] = temp1;
			drawTile(hitPos, impassCol, true);
			break;
		}
	}
	
	#region terrain
	//Creates terrain of various types
	void CreateTerrain (System.Random rand, TerrainGenType type)
	{
		tiles.Clear();
		for(int ii = 0; ii < width*height; ii++){
			tiles.Add(new Tile(TileState.PASSABLE,0, 100));
		}
		switch(type){
		case TerrainGenType.DIAMOND_SQUARE:
			CreateDiamondSquareTerrain (rand);
			break;
		case TerrainGenType.RANDOM:
			for(int ii = 0; ii < width*height; ii++){
				if(0==rand.Next()%2){
					Tile fuckingCSharp = tiles[ii];
					fuckingCSharp.state = TileState.IMPASSABLE;
					tiles[ii] = fuckingCSharp;
				}
			}
			break;
		}
	}
	
	//Redoes the terrain and redraws it
	void redoTerrain(){
		CreateTerrain(new System.Random(), terrainType);
		clearImage();
	}
	
	//Creates terrain using the diamond square algorithm
	//Assumes a power of two terrain
	//Resulting terrain tiles perfectly
	void CreateDiamondSquareTerrain (System.Random rand)
	{
		float maxHeight = 1;
		float minHeight = 0;
		float variance = (maxHeight - minHeight);
		Tile temp = new Tile(TileState.PASSABLE, 
			Mathf.Lerp(minHeight, maxHeight, 0.5f), 100);//Lazy MF
		//Outer Corners, everything is modulo width|height
		tiles[0] = temp;
		for(int step = width/2; step>0; step/=2, variance/=2.0f){
			//Diamond step
			for(int x = step; x<width; x+=2*step){
				for(int y = step; y<height; y+=2*step){
					float avg = 0;
					avg += tiles[tileIndex((width+x-step)%width, (height+y-step)%height)].height;
					avg += tiles[tileIndex((x+step)%width, (height+y-step)%height)].height;
					avg += tiles[tileIndex((x+step)%width, (y+step)%height)].height;
					avg += tiles[tileIndex((width+x-step)%width, (y+step)%height)].height;
					avg /= 4.0f;
					temp = tiles[tileIndex(x,y)];
					temp.height = avg + ((float)rand.NextDouble()*variance - variance/2.0f);
					tiles[tileIndex(x,y)] = temp;
				}
			}
			//Square step (f*cking complicated dance)
			bool odd = false;
			for(int x = 0; x<width; x+=step){
				for(int y = odd?0:step; y<height; y+=2*step){
					float avg = 0;
					avg += tiles[tileIndex(x, (height+y-step)%height)].height;
					avg += tiles[tileIndex(x, (y+step)%height)].height;
					avg += tiles[tileIndex((width+x-step)%width, y)].height;
					avg += tiles[tileIndex((x+step)%width, y)].height;
					avg /= 4.0f;
					temp = tiles[tileIndex(x,y)];
					temp.height = avg + ((float)rand.NextDouble()*variance - variance/2.0f);
					tiles[tileIndex(x,y)] = temp;
				}
				odd = !odd;//Hipsters' nightmare
			}
		}
		float avgHeight =0;
		for(int ii = 0; ii < tiles.Count; ii++)
			avgHeight += tiles[ii].height;
		avgHeight /= tiles.Count;
		for(int ii = 0; ii < tiles.Count; ii++){
			temp = tiles[ii];
			if(temp.height > avgHeight*1.25f || temp.height < avgHeight*0.75f)
				temp.state = TileState.IMPASSABLE;
			tiles[ii] = temp;
		}
	}
	#endregion terrain
	
	#region pathing
	// As each tile is visited and it's neighbours searched
	// for viability, it is added to the closed list
	void path(Int2 initTile, Int2 destTile){
		if(initTile == destTile)return;
		List<Int2> path = new List<Int2>();
		List<Int2> closed = new List<Int2>();
		int maxPathLen = 1000;
		int maxIters = 10000;
		int iters = 0;
		int backTrack = 0;
		path.Add(initTile);
		do{
			if(iters > maxIters)break;
			if(path.Count > maxPathLen){
				print("Hit max path length");
				break;
			}
			//Get the latest tile of the path
			if(path.Count - 1 - backTrack < 0){
				print ("back tracked past starting point");
				return;
			}
			Int2 currTile = path[path.Count - 1 - backTrack];
			//Add it to closed
			closed.Add(currTile);
			//search connections
			float bestCost = float.MaxValue;
			Int2 bestTile = currTile;
			for(int x = currTile.x - 1; x < currTile.x + 2; x++){
				for(int y = currTile.y - 1; y < currTile.y + 2; y++){
					Int2 searchTile = new Int2(x, y);
					if(isValidTile(searchTile) &&
						!closed.Contains(searchTile)){
						float currCost = tileCost(searchTile, destTile);
						if(currCost < bestCost){
							bestCost = currCost;
							bestTile = searchTile;
						}
					}
				}
			}
			if(bestTile == currTile){
				//go back to the last closed tile
				backTrack++;
				//Remove this deadend from the path
				path.RemoveAt(path.Count-1);
				if(path.Count == 0)return;
			}else{
				path.Add(bestTile);
				backTrack = 0;
			}
			iters++;
		}while(path[path.Count-1] != destTile);
		//drawPath(path);
		optimizePath(path);
		StartCoroutine(drawPathSlow(path));
		//drawPath(path);
	}
	
	IEnumerator AStar(Int2 start, Int2 destination){
		int maxIterations = 5000;
		int iterations = 1;
		Node startNode = new Node();
		startNode.tile = start;
		startNode.costSoFar = 0;
		startNode.estimatedTotalCost = intTileCost(start, destination);
		
		//Open list sorted by estimated total cost
		List<Node> open = new List<Node>();
		open.Add(startNode);
		//Closed nodes
		List<Node> closed = new List<Node>();
		Node current = new Node();
		//Dummy data to keep the compiler happy
		current.tile = new Int2(0,-1);
		
		//Some profiling
		float startTime = Time.realtimeSinceStartup;
		float timeTaken = 0;
		int timesWaited = 0;
		//while there are open nodes
		while(open.Count > 0){
			if(iterations > maxIterations)break;
			else if (iterations%200 == 0){
				timeTaken += Time.realtimeSinceStartup - startTime;
				timesWaited++;
				yield return new WaitForFixedUpdate();
				startTime = Time.realtimeSinceStartup;
			}
			iterations++;
			//get the smallest node in open
			current = open[0];
			
			//Have we reached the goal
			if(current.tile ==  destination)break;
			
			//Get it's connections
			List<Connection> connections = getConnections(current.tile);
			
			for(int ii = 0; ii < connections.Count; ii++){
				Node endNode = new Node();
				endNode.tile = connections[ii].toTile;
				int endNodeCost = current.costSoFar + connections[ii].cost;
				//Is it on the closed list
				int index = closed.FindIndex(new matchesTile(endNode.tile).Match);
				if(index != -1){
					endNode = closed[index];
					//if the new path isn't better skip it
					if(endNodeCost >= endNode.costSoFar)continue;
					//Remove it from closed because it needs to be revisited
					closed.RemoveAt(index);
					//Update costs
					endNode.estimatedTotalCost = endNodeCost +//New cost so far
						endNode.estimatedTotalCost - endNode.costSoFar;
					endNode.costSoFar = endNodeCost;
					endNode.prevTile = current.tile;
					//Add to open for reconsideration
					open.Add(endNode);
				}else{
					//Is it on the open list
					index = open.FindIndex(new matchesTile(endNode.tile).Match);
					if(index != -1){
						endNode = open[index];
						//if the new path isn't better skip it
						if(endNode.costSoFar >= endNodeCost)continue;
						//Update costs
						endNode.estimatedTotalCost = endNodeCost + //New cost so far
							endNode.estimatedTotalCost - endNode.costSoFar;
						endNode.costSoFar = endNodeCost;
						endNode.prevTile = current.tile;
						open.RemoveAt(index);
						open.Add(endNode);
						
					}else{
						//add it to the open list
						Node newNode = new Node();
						newNode.tile = connections[ii].toTile;
						newNode.costSoFar = endNodeCost;
						newNode.estimatedTotalCost = endNodeCost + intTileCost(newNode.tile, destination);
						newNode.prevTile = current.tile;
						open.Add(newNode);
					}					
				}
			}
			
			//add to the closed list
			closed.Add(open[0]);
			//remove from the open list
			open.RemoveAt(0);
			closed.Sort(new byEstimatedTotalCost());
			open.Sort(new byEstimatedTotalCost());
		}
		//Did we succeed?
		if(current.tile != destination){
			print ("A* failed to find a path");
			return false;
		}
		
		//Backtrack to create path
		List<Int2> path = new List<Int2>();
		while (current.tile != start){
			path.Add(current.tile);
			int index = closed.FindIndex(new matchesTile(current.prevTile).Match);
			try{
				current = closed[index];
			}catch(ArgumentOutOfRangeException){
				print ("Argument out of range");
			}
		}
		path.Add(current.tile);
		path.Reverse();
		reduceCostAlongPath(path);
		
		timeTaken += Time.realtimeSinceStartup - startTime;
		print ("Time taken: " + timeTaken + " Iterations: " + iterations + " Times Waited: " + timesWaited);
		StartCoroutine(drawPathSlow(path));
	}
	
	//Returns a list of connections to the surrounding tiles for A*
	List<Connection> getConnections(Int2 current){
		List<Connection> result = new List<Connection>(8);
		//Loop Y
		for(int x = (current.x-1) >= 0 ? (current.x-1):0;
			x < width && x < current.x+2; x++){
			//Loop Y
			for(int y = (current.y-1) >= 0 ? (current.y-1) : 0;
				y < height && y < current.y+2; y++){
				Int2 candidate = new Int2(x, y);
				//If it's a valid tile add a connection
				if(candidate != current && tiles[tileIndex(candidate)].state != TileState.IMPASSABLE){
					Connection newConnect = new Connection();
					newConnect.fromTile = current;
					newConnect.toTile = candidate;
					newConnect.cost = tiles[tileIndex(candidate)].pathingCost;
					//newConnect.cost = (current.x == candidate.x || current.y == candidate.y)//if it's straight
					//	? 100: 141;
					result.Add(newConnect);
				}
			}
		}
		result.TrimExcess();
		return result;
	}
	
	//Makes the tiles on a path cheaper creates actual beaten paths in the tiles
	void reduceCostAlongPath(List<Int2> path){
		for (int ii = 0; ii < path.Count; ii++){
			int index = tileIndex(path[ii]);
			Tile temp = tiles[index];
			int newCost = temp.pathingCost - 10;
			temp.pathingCost = newCost > 50 ? newCost : 50;
			tiles[index] = temp;
		}
	}
	
	//Removes unneccesary parts of the given path
	void optimizePath(List<Int2> path){
		//Move from the end of the list down
		for(int ii = path.Count-1; ii > 1; ii--){
			//Move from the start of the list up
			for(int jj = 0; jj < ii + 1; jj++){
				if(path[jj].isNextTo(path[ii])){
					//Remove redundant piece of path
					path.RemoveRange(jj+1, (ii-jj) - 1);
					//Fix index pointer
					ii = jj;
					break;
				}
			}
		}
		
	}
	
	//Draws a path between two random points at a set interval
	IEnumerator drawRandomPaths(){
		while(bDrawRandomPaths){
			Int2 firstTile = new Int2(-1, -1); 
			Int2 lastTile = new Int2(-1, -1);
			System.Random rand = new System.Random();
			int iters = 0;
			do{
				iters++;
				firstTile.x = rand.Next()%width;
				firstTile.y = rand.Next()%height;
			}while(!isValidTile(firstTile));
			
			do{
				iters++;
				lastTile.x = rand.Next()%width;
				lastTile.y = rand.Next()%height;
			}while(!isValidTile(lastTile) || firstTile == lastTile);
			drawTile(firstTile, Color.red, true);
			drawTile(lastTile, Color.red, true);
			StartCoroutine(AStar(firstTile, lastTile));
			//print ("random iterations: " + iters);
			yield return new WaitForSeconds(randomPathInterval);
		}
	}
	
	//simple straight line tile cost
	float tileCost(Int2 tile, Int2 dest){
		Int2 Dist = dest - tile;
		//return Mathf.Sqrt((float)(Dist.x * Dist.x + Dist.y * Dist.y));
		Dist.x = Dist.x < 0 ? -Dist.x : Dist.x;
		Dist.y = Dist.y < 0 ? -Dist.y : Dist.y;
		float min = Dist.x > Dist.y ? Dist.y : Dist.x;
		float max = Dist.x < Dist.y ? Dist.y : Dist.x;
		return max-min + min*1.414f; 
	}
	
	//Integer Tile cost
	int intTileCost(Int2 tile, Int2 dest){
		Int2 Dist = dest-tile;
		Dist.x = Dist.x < 0 ? -Dist.x : Dist.x;
		Dist.y = Dist.y < 0 ? -Dist.y : Dist.y;
		int min = Dist.x > Dist.y ? Dist.y : Dist.x;
		int max = Dist.x < Dist.y ? Dist.y : Dist.x;
		return (max-min)*100 + min*141; 
	}
	
	//Checks if a tile is within bounds and is passable
	bool isValidTile(Int2 tile){
		return tile.x >= 0 && tile.x < width && tile.y >= 0 && tile.y < height && 
			tiles[tileIndex(tile)].state == TileState.PASSABLE;
	}
	#endregion pathing
	
	#region drawing
	//Draw each tile in the path in green
	void drawPath(List<Int2> path){
		for(int ii = 0; ii < path.Count; ii++){
			drawTile(path[ii], Color.cyan, false);
		}
		tex.Apply();
		renderer.material.SetTexture("_MainTex",tex);
	}
	
	//Slowly draws the path
	IEnumerator drawPathSlow(List<Int2> path){
		for(int ii = 0; ii < path.Count; ii++){
			drawTile(path[ii], Color.green, true);
			yield return new WaitForSeconds(pathDrawStep);
		}
	}
	
	//Resets the image to the base free and blocked tiles
	void clearImage(){
		for(int x = 0; x < width; x++){
			for(int y = 0; y < height; y++){
				drawTile(new Int2(x,y),
					tiles[y*width + x].state == TileState.PASSABLE?
					passCol:impassCol, false);
			}
		}
		tex.Apply();
	}
	
	//Shows the height 0 tiles in magenta
	void showZeroHeightTiles(){
		for(int ii = 0; ii < tiles.Count; ii++){
			if(tiles[ii].height == 0){
				drawTile(ii/width, ii%width, Color.magenta, false);
			}
		}
		tex.Apply();
	}
	
	void drawPathingCosts(){
		for(int ii = 0; ii < tiles.Count; ii++){
			if(tiles[ii].state == TileState.IMPASSABLE)
				continue;
			drawTile(ii%width, ii/width, 
				new Color(tiles[ii].pathingCost / 100.0f, 0, 0, 1), false);
		}
		tex.Apply();
	}
	
	//Draws a single tile onto 'tex' and applies it if 'apply' is true
	void drawTile(Int2 tile, Color col, bool apply = true){
		Color current = tex.GetPixel(tile.x*tileScale, tile.y*tileScale);
		col.r = Mathf.Lerp(current.r, col.r, col.a);
		col.b = Mathf.Lerp(current.b, col.b, col.a);
		col.g = Mathf.Lerp(current.g, col.g, col.a);
		for(int x = tile.x * tileScale; x < tile.x * tileScale + tileScale; x++){
			for(int y = tile.y * tileScale; y < tile.y * tileScale + tileScale; y++)
				tex.SetPixel(x, y, col);
		}
		if(apply)tex.Apply();
	}
	void drawTile(int x, int y, Color col, bool apply = true){
		drawTile(new Int2(x, y), col, apply);
	}
	#endregion drawing
	
	//index lookup
	int tileIndex(Int2 pos){
		return tileIndex(pos.x, pos.y);
	}
	
	//index lookup
	int tileIndex(int x, int y){
		return width*y + x;
	}
	
	//moves a number closer to 0 or 1 in a smooth curve
	float exagerateNormalized(float s){
		s = Mathf.Clamp01(s);
		return 3*(Mathf.Pow(s, 2.0f)) - 2*(Mathf.Pow(s, 3));
	}
}
