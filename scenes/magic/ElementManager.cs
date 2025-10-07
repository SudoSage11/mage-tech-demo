#nullable disable

using Config;
using Godot;
using MageTechDemo.scripts;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Responsible for managing control nodes for each element, including physics processing and rendering.
/// 
/// <para>Should only have one per level/world.</para>
/// </summary>
public partial class ElementManager : Node3D
{
	#region Properties
	/// <summary>
	/// How far apart each mesh point is from each other in meters.
	/// </summary>
	[Export(PropertyHint.Range, "0.05,1,0.05")]
	protected float MeshPointSpacing { get; set; } = 0.2f;

	/// <summary>
	/// The radius away from each control point that mesh points will be generated in meters.
	/// </summary>
	[Export(PropertyHint.Range, "0,2,0.05")]
	protected float MeshGenerationRadius { get; set; } = 0.5f;

	/// <summary>
	/// The radius away from each control point that mesh points will be generated in meters.
	/// </summary>
	[Export(PropertyHint.Range, "0,2,0.05")]
	protected float DrawPoint { get; set; } = 0.5f;

	#endregion

	#region Variables

	//* CLASS VARIABLES

	Dictionary<int, MeshPoint> fireMeshPoints = [];

	//* NODES
	private Node3D controlPoints;
	private MeshInstance3D fireMeshInstance;

	private MeshInstance3D debugPointMeshInstance;
	private MeshInstance3D debugLineMeshInstance;

	#endregion

	#region Overrides
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		controlPoints = GetNode<Node3D>("ControlPoints");
		debugLineMeshInstance = GetNode<MeshInstance3D>("DebugLinesMeshInstance");
		debugPointMeshInstance = GetNode<MeshInstance3D>("DebugPointsMeshInstance");

		fireMeshInstance = GetNode<MeshInstance3D>("FireMeshInstance");
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		BuildMeshPoints();
	}
	#endregion

	#region Drawing

	/// <summary>
	/// Rebuilds the mesh points with weight values based on the type of element.
	/// </summary>
	private void BuildMeshPoints()
	{
		fireMeshPoints.Clear();

		if (controlPoints == null) return;

		// Loop through each control point and create mesh points around it.
		foreach (ControlPoint control in controlPoints.GetChildren().Cast<ControlPoint>())
		{
			Vector3 controlPos = control.Position;

			float offset = MeshGenerationRadius + (MeshPointSpacing * 2);

			// Iterate through a grid around each control point
			for (float x = controlPos.X - offset; x < controlPos.X + offset; x += MeshPointSpacing)
			{
				for (float y = controlPos.Y - offset; y < controlPos.Y + offset; y += MeshPointSpacing)
				{
					for (float z = controlPos.Z - offset; z < controlPos.Z + offset; z += MeshPointSpacing)
					{
						Vector3 meshPointPos = new(
							x.RoundToNearestGivenFloat(MeshPointSpacing),
							y.RoundToNearestGivenFloat(MeshPointSpacing),
							z.RoundToNearestGivenFloat(MeshPointSpacing)
						);
						float distanceToControl = meshPointPos.DistanceTo(controlPos);

						// Check if the point is within the radius to ignore points outside the radius.
						if (distanceToControl < MeshGenerationRadius + (MeshPointSpacing * 2))
						{
							float weight = control.CalculateMeshPointWeight(meshPointPos);
							MeshPoint point = new(meshPointPos, weight);

							// If already in the list but the saved weight is lower, update the weight
							if (fireMeshPoints.TryGetValue(point.PosHash, out MeshPoint value))
							{
								if (value.Weight < weight)
								{
									fireMeshPoints[point.PosHash].Weight = weight;
								}
							}
							else
							{
								fireMeshPoints.Add(point.PosHash, point);
							}
						}
					}
				}
			}
		}

		// Draws debug mesh points
		DrawMeshPoints();

		// Draw meshes
		DrawMesh(fireMeshInstance, fireMeshPoints);
	}

	private void DrawMesh(MeshInstance3D meshInstance, Dictionary<int, MeshPoint> meshPoints)
	{
		ImmediateMesh mesh = meshInstance.Mesh as ImmediateMesh;
		mesh.ClearSurfaces();

		ImmediateMesh normalMesh = debugLineMeshInstance.Mesh as ImmediateMesh;
		normalMesh.ClearSurfaces();

		// If not checked, just clear the mesh. If there aren't any points nothing is going to get drawn
		if (!ConfigSettings.Instance.DrawGeneratedMeshes || meshPoints.Count == 0) return;

		if (ConfigSettings.Instance.DrawDebugLines)
		{
			normalMesh.SurfaceBegin(Mesh.PrimitiveType.Lines);
		}

		mesh.SurfaceBegin(Mesh.PrimitiveType.Triangles);

		// Render mesh from points
		foreach (MeshPoint point in meshPoints.Values)
		{
			Vector3 cubeOrigin = point.Position;

			List<Vector3> cubeCorners = [];
			List<float> cubeWeights = [];

			for (int v = 0; v < 8; v++)
			{
				Vector3 offsetPos = cubeOrigin + (GenerationTable.originOffsets[v] * MeshPointSpacing);

				cubeCorners.Add(offsetPos);

				if (meshPoints.TryGetValue(VectorUtils.GetVectorHash(offsetPos), out MeshPoint offsetMarker))
				{
					cubeWeights.Add(offsetMarker.Weight);
				}
				else
				{
					cubeWeights.Add(-1f);
				}
			}

			Cube cube = new([.. cubeCorners], [.. cubeWeights], DrawPoint);

			var triangles = cube.GetTriangles();

			if (triangles.Count == 0)
			{
				continue;
			}

			foreach (Triangle tri in triangles)
			{
				// Calculate the normal of the whole face, and apple to each vertex
				Vector3 vertA = tri.A;
				Vector3 vertB = tri.B;
				Vector3 vertC = tri.C;

				Vector3 normal = (vertC - vertA).Cross(vertB - vertA);
				normal = normal.Normalized();

				float avgX = (vertA.X + vertB.X + vertC.X) / 3;
				float avgY = (vertA.Y + vertB.Y + vertC.Y) / 3;
				float avgZ = (vertA.Z + vertB.Z + vertC.Z) / 3;

				Vector3 triAvg = new(avgX, avgY, avgZ);

				// Smooth normals from the scalar field gradient
				Vector3 smoothA = ComputeSmoothNormal(vertA, MeshPointSpacing);
				Vector3 smoothB = ComputeSmoothNormal(vertB, MeshPointSpacing);
				Vector3 smoothC = ComputeSmoothNormal(vertC, MeshPointSpacing);

				// Because a single vertex can only have one normal, we use the real normal property for the flat normal (lighting)
				// and encode the smooth normal (vertex displacement) in the UV2 property. Hacky, but it works.

				mesh.SurfaceSetUV2(VectorUtils.EncodeOctahedron(smoothA));
				mesh.SurfaceSetNormal(normal);
				mesh.SurfaceAddVertex(vertA);

				mesh.SurfaceSetUV2(VectorUtils.EncodeOctahedron(smoothB));
				mesh.SurfaceSetNormal(normal);
				mesh.SurfaceAddVertex(vertB);

				mesh.SurfaceSetUV2(VectorUtils.EncodeOctahedron(smoothC));
				mesh.SurfaceSetNormal(normal);
				mesh.SurfaceAddVertex(vertC);

				if (ConfigSettings.Instance.DrawDebugLines)
				{
					normalMesh.SurfaceSetColor(Colors.LimeGreen);
					normalMesh.SurfaceAddVertex(triAvg);

					normalMesh.SurfaceSetColor(Colors.LimeGreen);
					normalMesh.SurfaceAddVertex(triAvg + (normal * 0.2f));
				}
			}
		}

		mesh.SurfaceEnd();

		if (ConfigSettings.Instance.DrawDebugLines)
		{
			normalMesh.SurfaceEnd();
		}
	}

	private void DrawMeshPoints()
	{
		ImmediateMesh debugMesh = debugPointMeshInstance.Mesh as ImmediateMesh;
		debugMesh.ClearSurfaces();

		// If not checked, just clear the mesh
		if (!ConfigSettings.Instance.DrawDebugPoints) return;

		debugMesh.SurfaceBegin(Mesh.PrimitiveType.Points);

		foreach (MeshPoint point in fireMeshPoints.Values)
		{
			debugMesh.SurfaceSetColor(point.GetColor(DrawPoint));
			debugMesh.SurfaceAddVertex(point.Position);
		}

		debugMesh.SurfaceEnd();
	}

	float GetNodeWeight(Vector3 nodePos)
	{
		return fireMeshPoints.TryGetValue(VectorUtils.GetVectorHash(nodePos), out var mp)
				? mp.Weight
				: -1f; // background
	}

	// Sample the scalar field (metaball / density function) at any position.
	// This version interpolates the meshPoints dictionary by trilinear interpolation.
	float SampleScalarField(Vector3 position, float spacing)
	{
		// Snap to the nearest cell corner
		Vector3 cellOrigin = new(
				Mathf.Floor(position.X / spacing) * spacing,
				Mathf.Floor(position.Y / spacing) * spacing,
				Mathf.Floor(position.Z / spacing) * spacing
		);

		// Local coordinates in the cell [0..1]
		Vector3 localFraction = (position - cellOrigin) / spacing;

		// Fetch corner weights
		float w000 = GetNodeWeight(cellOrigin + new Vector3(0, 0, 0));
		float w100 = GetNodeWeight(cellOrigin + new Vector3(spacing, 0, 0));
		float w010 = GetNodeWeight(cellOrigin + new Vector3(0, spacing, 0));
		float w110 = GetNodeWeight(cellOrigin + new Vector3(spacing, spacing, 0));
		float w001 = GetNodeWeight(cellOrigin + new Vector3(0, 0, spacing));
		float w101 = GetNodeWeight(cellOrigin + new Vector3(spacing, 0, spacing));
		float w011 = GetNodeWeight(cellOrigin + new Vector3(0, spacing, spacing));
		float w111 = GetNodeWeight(cellOrigin + new Vector3(spacing, spacing, spacing));

		// Trilinear interpolation
		float w00 = Mathf.Lerp(w000, w100, localFraction.X);
		float w10 = Mathf.Lerp(w010, w110, localFraction.X);
		float w01 = Mathf.Lerp(w001, w101, localFraction.X);
		float w11 = Mathf.Lerp(w011, w111, localFraction.X);

		float w0 = Mathf.Lerp(w00, w10, localFraction.Y);
		float w1 = Mathf.Lerp(w01, w11, localFraction.Y);

		return Mathf.Lerp(w0, w1, localFraction.Z);
	}

	// Compute the smooth surface normal at a vertex position by sampling
	// the scalar field gradient using central differences.
	Vector3 ComputeSmoothNormal(Vector3 position, float spacing)
	{
		float offset = spacing * 0.5f; // step size for finite difference

		float sampleXPlus = SampleScalarField(position + new Vector3(offset, 0, 0), spacing);
		float sampleXMinus = SampleScalarField(position - new Vector3(offset, 0, 0), spacing);

		float sampleYPlus = SampleScalarField(position + new Vector3(0, offset, 0), spacing);
		float sampleYMinus = SampleScalarField(position - new Vector3(0, offset, 0), spacing);

		float sampleZPlus = SampleScalarField(position + new Vector3(0, 0, offset), spacing);
		float sampleZMinus = SampleScalarField(position - new Vector3(0, 0, offset), spacing);

		Vector3 gradient = new(
				(sampleXPlus - sampleXMinus) / (2f * offset),
				(sampleYPlus - sampleYMinus) / (2f * offset),
				(sampleZPlus - sampleZMinus) / (2f * offset)
		);

		return gradient.Length() > 0f ? gradient.Normalized() : Vector3.Zero;
	}
	#endregion

	#region Element Management
	public void CreateFireElement(Vector3 position)
	{
		FireControlPoint firePoint = new();
		firePoint.Position = position;

		controlPoints.AddChild(firePoint);
	}
	#endregion
}
