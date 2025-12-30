using UnityEngine;

public class FastRandom
{
	private uint state;
    
	public FastRandom(uint seed)
	{
		state = seed;
	}
    
	public float NextFloat()
	{
		state = state * 1664525u + 1013904223u;
		return (state & 0x00FFFFFF) / (float)0x01000000;
	}
    
	public Vector2 NextFloat2()
	{
		return new Vector2(NextFloat(), NextFloat());
	}
}