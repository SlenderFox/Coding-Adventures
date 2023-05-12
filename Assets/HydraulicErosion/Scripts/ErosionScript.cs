using UnityEngine;

namespace HydraulicErosionProj
{
	[System.Serializable]
	public class ErosionScript
	{
		/// <summary>
		/// The Droplet struct handles all the data for each droplet
		/// </summary>
		public struct Droplet
		{
			public Droplet(Vector2Int pPosition, float pHeight, float pWater)
			{
				position = pPosition;
				height = pHeight;
				water = pWater;
				prevHeight = float.PositiveInfinity;
				sediment = 0;
				inertia = 0;    // Inertia is just the delta height
			}

			public Vector2Int position;
			public float height;
			public float water;
			public float prevHeight;
			public float sediment;
			public float inertia;

			// Sediment capacity is how much water is left
		}

		[SerializeField]
		private ComputeShader m_erosionShader = null;

		[Min(1), Tooltip("The maximum amount of iterations done per droplet")]
		public short m_maxLifetime = 90;

		[Range(0.001f, 0.3f), Tooltip("How fast each droplet collects sediment from the ground")]
		public float m_erosionSpeed = 0.01f;

		[Min(0.01f), Tooltip("How much water the droplets will start with," +
			" more = more sediment carried")]
		public float m_startWater = 2;

		[Tooltip("How much water is lost each step")]
		public float m_evaporationRate = 0.01f;

		[Tooltip("The amount of droplets sequentially simulated on the cpu. VERY SLOW.")]
		public uint m_numDroplets = 10000;

		[Tooltip("The amount of groups requested for the gpu. " +
			"Droplets simulated = groups * 1024")]
		public uint m_numGroups = 10;

		public void RunErosion(HydraulicErosionMaster _master, ushort _resolution)
		{
			// Loop through each and every droplet (slow)
			for (int i = 0; i < m_numDroplets; ++i)
			{
				// Initialise a new droplet
				Vector2Int startPosition = new Vector2Int(
					Random.Range(0, _resolution),
					Random.Range(0, _resolution)
				);
				Droplet drop = new Droplet(startPosition, m_startWater, _master.GetFromHeightMap(startPosition));

				// Each loop is a step in the droplets life (slow)
				// A break in this loop is equivelant to the droplet dying
				for (int j = 0; j < m_maxLifetime; ++j)
				{
					// Update the previous height
					drop.prevHeight = drop.height;

					// Calculate the lowest adjacent point and update the droplet position to it
					Vector2Int lowestPos = new Vector2Int(int.MaxValue, int.MaxValue);
					float lowestHeight = int.MaxValue;
					Vector2Int cyclePos;
					float cycleHeight;

					for (int g = 0; g < 8; g++)
					{
						// Rotates counter-clockwise
						cyclePos = g switch
						{
							0 => new Vector2Int(drop.position.x - 1, drop.position.y - 1),
							1 => new Vector2Int(drop.position.x,     drop.position.y - 1),
							2 => new Vector2Int(drop.position.x + 1, drop.position.y - 1),
							3 => new Vector2Int(drop.position.x + 1, drop.position.y    ),
							4 => new Vector2Int(drop.position.x + 1, drop.position.y + 1),
							5 => new Vector2Int(drop.position.x,     drop.position.y + 1),
							6 => new Vector2Int(drop.position.x - 1, drop.position.y + 1),
							_ => new Vector2Int(drop.position.x - 1, drop.position.y    ),
						};
						cycleHeight = _master.GetFromHeightMap(cyclePos);
						if (cycleHeight < drop.height && cycleHeight < lowestHeight)
						{
							lowestPos = cyclePos;
							lowestHeight = cycleHeight;
						}
					}

					// Droplet is at the lowest position of the surrounding spots
					if (drop.height <= lowestHeight)
					{
						// Disbtribute the sediment to the surrounding points aswell
						_master.ModifyHeightMap(ref drop, drop.sediment);
						break;
					}

					// Update the droplets position to the lowest adjacent point
					drop.position = lowestPos;
					// Update the droplets height
					drop.height = lowestHeight;
					// inertia is the delta height
					drop.inertia = drop.prevHeight - drop.height;

					// This is all a mess
					// Find the delta speed (possibly wrong)
					//deltaSpeed = -deltaHeight;
					//// Update the speed
					//drop.speed += deltaSpeed; // FIX THIS
					//// Calculate the new sediment capacity based on remaining water and droplet speed
					//drop.sedimentCapacity = (drop.water * drop.speed < m_fMaxSedimentCapacity)
					//    ? drop.water * drop.speed : m_fMaxSedimentCapacity;

					// Evaporate some water
					drop.water -= Mathf.Min(m_evaporationRate, drop.water);

					if (drop.water <= 0)
					{
						break;
					}

					// TODO: Do the shit where soil is taken and deposited in an area around a point
					if (drop.sediment > drop.water)
					{
						// Deposit some soil
						_master.ModifyHeightMap(ref drop, drop.water - drop.sediment);
					}
					else
					{
						// Erode some soil
						float sedimentTaken = Mathf.Min(m_erosionSpeed, drop.inertia);
						_master.ModifyHeightMap(ref drop, -sedimentTaken);
					}
				}
			}
		}

		public void RunErosionComputeShader(HydraulicErosionMaster _master, ushort _resolution)
		{
		//	// Sets the compute shader data
		//	ComputeBuffer heightBuffer = new ComputeBuffer(m_heightMap.heightMap.Length, sizeof(float));
		//	heightBuffer.SetData(m_heightMap.heightMap);
		//	m_erosionShader.SetBuffer(0, "heightMap", heightBuffer);
		//	m_erosionShader.SetInt("resolution", m_resolution);
		//	m_erosionShader.SetInt("maxLifetime", m_maxLifetime);
		//	m_erosionShader.SetFloat("erosionSpeed", m_erosionSpeed);
		//	m_erosionShader.SetFloat("evaporationRate", m_evaporationRate);
		//	m_erosionShader.SetFloat("water", m_startWater);

		//	// Dispach the compute shader
		//	m_erosionShader.Dispatch(0, m_numGroups, 1, 1);

		//	// Retrieve the data and release the buffer
		//	heightBuffer.GetData(m_heightMap.heightMap);
		//	heightBuffer.Release();
		}

		/// <summary>
		/// Used to display the number of droplets run on the cpu on the button
		/// </summary>
		/// <returns>How many droplets are going to be simulated on the cpu</returns>
		public uint GetNumberOfDroplets() { return m_numDroplets; }

		/// <summary>
		/// Used to display the number of droplets run on the gpu on the button
		/// </summary>
		/// <returns>How many droplets are going to be simulated on the gpu</returns>
		public uint GetComputeShaderThreads() { return m_numGroups * 1024; }
	}
}
