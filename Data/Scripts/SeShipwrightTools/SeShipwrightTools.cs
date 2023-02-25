using System;
using System.Collections.Generic;
using Sandbox.Definitions;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Weapons;
using Sandbox.ModAPI.Interfaces;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ObjectBuilders;
using VRage.Utils;
using VRageMath;
using Sandbox.Common.ObjectBuilders;
//using BlendTypeEnum = VRageRender.MyBillboard.BlendTypeEnum;

namespace Chozabu.ConveyorReplacer
{
	[MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
	public class ArmorReplacer : MySessionComponentBase
	{

		struct replaceinfo
		{
			public IMySlimBlock block;
			public MyObjectBuilder_CubeBlock objectBuilder;

		}

		public override void Init(MyObjectBuilder_SessionComponent sessionComponent)
		{
			MyAPIGateway.Utilities.MessageEntered += Utilities_MessageEntered;
		}

		private void Utilities_MessageEntered(string messageText, ref bool sendToOthers)
		{
			if (messageText == "/conv" && MyAPIGateway.Session.IsServer)
			{
				MyAPIGateway.Utilities.ShowMessage("", "conv converting");
				ReplaceAllConveyorBlocks();
				sendToOthers = false;
				MyAPIGateway.Utilities.ShowMessage("", "conv converted");
			}
			if (messageText == "/convp" && MyAPIGateway.Session.IsServer)
			{
				MyAPIGateway.Utilities.ShowMessage("", "conv converting");
				ReplaceAllConveyorBlocks("p");
				sendToOthers = false;
				MyAPIGateway.Utilities.ShowMessage("", "conv converted");
			}
			if (messageText == "/convr" && MyAPIGateway.Session.IsServer)
			{
				MyAPIGateway.Utilities.ShowMessage("", "conv converting");
				ReplaceAllConveyorBlocks("r");
				sendToOthers = false;
				MyAPIGateway.Utilities.ShowMessage("", "conv converted");
			}

			if (messageText == "/names" && MyAPIGateway.Session.IsServer)
			{
				MyAPIGateway.Utilities.ShowMessage("", "blocknames");
				PrintBlockNames();
				sendToOthers = false;
			}
			if (messageText == "/showarmor" && MyAPIGateway.Session.IsServer)
			{
				MyAPIGateway.Utilities.ShowMessage("", "ShowingArmor");
				SetArmorVisibility(true);
				sendToOthers = false;
			}
			if (messageText == "/hidearmor" && MyAPIGateway.Session.IsServer)
			{
				MyAPIGateway.Utilities.ShowMessage("", "HidingArmor");
				SetArmorVisibility(false);
				sendToOthers = false;
			}

		}
		public IMyCubeGrid GetTargetedGrid()
		{
			var MaxRaycastDistance = 10000;
			var player = MyAPIGateway.Session?.LocalHumanPlayer;
			if (player == null) return null;

			var cameraMatrix = MyAPIGateway.Session.Camera.WorldMatrix;
			var forwardVector = Vector3D.Normalize(cameraMatrix.Forward);
			var toVector = cameraMatrix.Translation + forwardVector * MaxRaycastDistance;

			IHitInfo hitInfo;
			if (!MyAPIGateway.Physics.CastRay(cameraMatrix.Translation, toVector, out hitInfo))
				return null;

			var entity = hitInfo?.HitEntity;
			if (entity?.Physics == null) return null;

			var grid = entity.GetTopMostParent() as IMyCubeGrid;
			return grid;
		}

		public void SetArmorVisibility(bool showBlock)
		{
			float transparancy = 0.0f;
			if (!showBlock) transparancy = 0.8f;
			IMyCubeGrid grid = GetTargetedGrid();
			if (grid == null) return;

			List<IMySlimBlock> allBlocks = new List<IMySlimBlock>();

			grid.GetBlocks(allBlocks);

			// Replace each conveyor block with the correct type
			foreach (IMySlimBlock block in allBlocks)
			{

				if (block.GetObjectBuilder().SubtypeName.Contains("Armor"))// && block.FatBlock != null)
				{
					MyAPIGateway.Utilities.ShowMessage("", block.GetObjectBuilder().SubtypeName);
					//attmpt1
					//block.FatBlock.Render.Visible = showBlock;

					//attempt2
					//block.FatBlock.Render.Transparency = transparancy;
					//block.FatBlock.Render.UpdateTransparency();

					//attempt3
					//block.FatBlock.Transparent = !showBlock;

					//attempt4... no fatblock on armor?
					/*var render = block.FatBlock.Render;
					if (render != null)
					{
						//var transparency = !showBlock ? 0.5f : 0f;
						render.Transparency = transparancy;
					}*/

					block.Dithering = transparancy;
				}
			}

		}

		public void PrintBlockNames()
		{
			IMyCubeGrid grid = GetTargetedGrid();
			if (grid == null) return;

			List<IMySlimBlock> allBlocks = new List<IMySlimBlock>();

			grid.GetBlocks(allBlocks);

			// Replace each conveyor block with the correct type
			foreach (IMySlimBlock block in allBlocks)
			{

				MyAPIGateway.Utilities.ShowMessage("", block.GetObjectBuilder().SubtypeName);
			}

		}
		public void ReplaceAllConveyorBlocks(string tubetype = "t")
		{
			IMyCubeGrid grid = GetTargetedGrid();
			if (grid == null) return;

			// Find all conveyor blocks on the grid
			List<IMySlimBlock> conveyorBlocks = new List<IMySlimBlock>();
			List<replaceinfo> replaceList = new List<replaceinfo>();

			//grid.GetBlocks(conveyorBlocks, block => block.FatBlock != null && block.FatBlock.BlockDefinition.TypeId.ToString() == "MyObjectBuilder_Conveyor");
			grid.GetBlocks(conveyorBlocks, block => block.FatBlock != null && block.FatBlock.BlockDefinition.TypeId.ToString().Contains("Conveyor"));
			

			// Replace each conveyor block with the correct type
			foreach (IMySlimBlock conveyorBlock in conveyorBlocks)
			{
				replaceinfo blockinfo;
				blockinfo.block = conveyorBlock;
				blockinfo.objectBuilder = GetBuilderToReplaceConveyor(conveyorBlock, tubetype);
				// Replace the conveyor block
				replaceList.Add(blockinfo);
			}

			foreach (replaceinfo ri in replaceList)
			{
				grid.RemoveBlock(ri.block);
				grid.AddBlock(ri.objectBuilder, true);
			}
		}

		public bool HasMatchingAxis(IMySlimBlock block1, IMySlimBlock block2)
		{
			Vector3I pos1 = block1.Position;
			Vector3I pos2 = block2.Position;

			if (pos1.X == pos2.X && pos1.Y == pos2.Y) return true;
			if (pos1.Z == pos2.Z && pos1.Y == pos2.Y) return true;
			if (pos1.X == pos2.X && pos1.Z == pos2.Z) return true;
			return false;

		}

		public Vector3 getPerpAxis(IMySlimBlock block1, IMySlimBlock block2)
		{
			Vector3I pos1 = block1.Position;
			Vector3I pos2 = block2.Position;

			if (pos1.X == pos2.X && pos1.Y == pos2.Y) return new Vector3(1, 0, 0);
			if (pos1.Z == pos2.Z && pos1.Y == pos2.Y) return new Vector3(0, 1, 0);
			if (pos1.X == pos2.X && pos1.Z == pos2.Z) return new Vector3(0, 0, 1);
			return new Vector3(1, 0, 0);

		}
		//based on digis code at: https://github.com/THDigi/BuildInfo/blob/ac69e815a9c82e3d0e27013f86109af556a1f503/Data/Scripts/BuildInfo/Features/LiveData/LiveDataHandler.cs#L178
		bool CheckConveyorSupport(IMyCubeBlock block)
		{
			if (block == null) return false;

			Type[] interfaces = MyAPIGateway.Reflection.GetInterfaces(block.GetType());
			bool supportsConveyors = false;

			for (int i = (interfaces.Length - 1); i >= 0; i--)
			{
				Type iface = interfaces[i];
				if (iface.Name == "IMyConveyorEndpointBlock")
				{
					supportsConveyors = true;
					break;
				}
				else if (iface.Name == "IMyConveyorSegmentBlock")
				{
					supportsConveyors = true;
					break;
				}
			}

			return supportsConveyors;
		}
		//todo check if port is in right direction: https://github.com/THDigi/BuildInfo/blob/master/Data/Scripts/BuildInfo/Features/LiveData/Base.cs#L238

		public List<IMySlimBlock> GetConveyorNeighbors(IMySlimBlock block)
		{
			List<IMySlimBlock> conveyorBlocks = new List<IMySlimBlock>();
			List<IMySlimBlock> neighbors = new List<IMySlimBlock>();
			block.GetNeighbours(neighbors);
			//block.CubeGrid.GetBlocks(neighbors, b => b != null && b != block && b.FatBlock != null);

			foreach (IMySlimBlock neighbor in neighbors)
			{
				if (neighbor.GetObjectBuilder().SubtypeName.Contains("Conveyor"))
				//if (neighbor.FatBlock is IMyConveyor)
				{
					conveyorBlocks.Add(neighbor);
				} else
                {
                    //neighbor.FatBlock.CheckConnectionAllowed
                    if (CheckConveyorSupport(neighbor.FatBlock)) {
					//if (conveyorBlock != null)
					//{
						conveyorBlocks.Add(neighbor);
					}
				}
			}

			return conveyorBlocks;
		}

		public MyObjectBuilder_CubeBlock GetBuilderToReplaceConveyor(IMySlimBlock block, string tubetype)
		{

			string junctionS = "LargeBlockConveyor";
			string straightS = "ConveyorTube";
			string cornerS = "ConveyorTubeCurved";
			string tS = "ConveyorTubeT";
			if (tubetype.Equals("p"))
			{
				junctionS = "LargeBlockConveyorPipeJunction";
				straightS = "LargeBlockConveyorPipeSeamless";
				cornerS = "LargeBlockConveyorPipeCorner";
				tS = "LargeBlockConveyorPipeT";
			}
			else if (tubetype.Equals("r"))
			{
				junctionS = "LargeBlockConveyor";
				straightS = "ConveyorTubeDuct";
				cornerS = "ConveyorTubeDuctCurved";
				tS = "ConveyorTubeDuctT";

			}

			var surroundings = GetConveyorNeighbors(block);
			var snum = surroundings.Count;
			var ConveyorType = "LargeBlockConveyor";

			var orientation = block.Orientation;

			var forward = Base6Directions.GetVector(orientation.Forward);
			var up = Base6Directions.GetVector(orientation.Up);
			var right = Vector3.Cross(forward, up); //for local calc only
			MyAPIGateway.Utilities.ShowMessage("", "block nei num " + snum);
			if (snum == 1 || snum == 0)
			{// dead end or isolated conveyor
			 //leave as-is, should perhaps set to junction?
			}
			else if (snum == 2)
			{ //straight or corner
				if (HasMatchingAxis(surroundings[0], surroundings[1]))
				{
					ConveyorType = straightS;

					//up = surroundings[0].Position - block.Position;
					//right = up + new Vector3(1, 1, 1); //lame method
					//forward = Vector3.Cross(up, right);

					var a = surroundings[0].Position - block.Position;
					var b = getPerpAxis(surroundings[0], surroundings[1]);
					var c = Vector3.Cross(a, b);


					if (tubetype.Equals("r"))/// why are ducts rotated differently :(
					{
						forward = c;
						up = b;
					}
					else
					{
						forward = b;
						up = a;
					}
				}
				else
				{
					ConveyorType = cornerS;
					up = block.Position - surroundings[0].Position;
					right = surroundings[1].Position - block.Position;
					forward = Vector3.Cross(up, right);

				}
			}
			else if (snum == 3)
			{ //T junction if two on same line
				var line01 = HasMatchingAxis(surroundings[0], surroundings[1]);
				var line02 = HasMatchingAxis(surroundings[0], surroundings[2]);
				var line12 = HasMatchingAxis(surroundings[1], surroundings[2]);
				if (line01 || line02 || line12)
				{
					var a = surroundings[0];
					var b = surroundings[1];
					var c = surroundings[2];
					ConveyorType = tS;
					if (line01)
					{
					}
					if (line02)
					{
						a = surroundings[0];
						b = surroundings[2];
						c = surroundings[1];
					}
					if (line12)
					{
						a = surroundings[1];
						b = surroundings[2];
						c = surroundings[0];
					}

					right = b.Position - a.Position;
					up = block.Position - c.Position;
					forward = Vector3.Cross(right, up) * -1;
				}
				else
				{
					//cant do T, should do junction
					ConveyorType = junctionS;

				}
			}
			else
			{
				//4 or more, set to junction
				ConveyorType = junctionS;
			}

			return GetBuilderWithSpecs(block.CubeGrid, block, ConveyorType, up, forward);

		}
		public MyObjectBuilder_CubeBlock GetBuilderWithSpecs(IMyCubeGrid grid, IMySlimBlock blockToReplace, string conveyorType, Vector3D upVector, Vector3D forwardVector)
		{
			Vector3I position = blockToReplace.Position;

			var forwardDir = Base6Directions.GetClosestDirection(forwardVector);
			var upDir = Base6Directions.GetClosestDirection(upVector);

			var orientation = new MyBlockOrientation(forwardDir, upDir);

			//MyObjectBuilder_CubeBlock objectBuilder = MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_CubeBlock>(conveyorType);
			MyObjectBuilder_CubeBlock objectBuilder = blockToReplace.GetObjectBuilder();
			MyAPIGateway.Utilities.ShowMessage("", "replacing... " + objectBuilder.SubtypeName + " with " + conveyorType);
			//MyObjectBuilder_CubeBlock objectBuilder = blockToReplace.GetObjectBuilder(true);

			objectBuilder.SubtypeName = conveyorType;
			objectBuilder.EntityId = 0;
			//objectBuilder.Min = position;
			objectBuilder.BlockOrientation = orientation;

			return objectBuilder;
		}

		protected override void UnloadData()
		{
			MyAPIGateway.Utilities.MessageEntered -= Utilities_MessageEntered;
		}
	}
}