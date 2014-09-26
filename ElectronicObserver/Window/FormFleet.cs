﻿using ElectronicObserver.Data;
using ElectronicObserver.Resource;
using ElectronicObserver.Utility.Mathematics;
using ElectronicObserver.Window.Control;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;

namespace ElectronicObserver.Window {

	public partial class FormFleet : DockContent {

		private class TableFleetControl {
			public Label Name;
			public ImageLabel StateMain;
			public ImageLabel AirSuperiority;
			public ImageLabel SearchingAbility;

			public TableFleetControl( FormFleet parent ) {

				#region Initialize

				Name = new Label();
				Name.Text = "[" + parent.FleetID.ToString() + "]";
				Name.Anchor = AnchorStyles.Left;
				Name.Font = parent.MainFont;
				Name.ForeColor = parent.MainFontColor;
				Name.Padding = new Padding( 0, 1, 0, 1 );
				Name.Margin = new Padding( 2, 0, 2, 0 );
				Name.AutoSize = true;
				//Name.Visible = false;

				StateMain = new ImageLabel();
				StateMain.Anchor = AnchorStyles.Left;
				StateMain.Font = parent.MainFont;
				StateMain.ForeColor = parent.MainFontColor;
				StateMain.ImageList = ResourceManager.Instance.Icons;
				StateMain.Padding = new Padding( 2, 2, 2, 2 );
				StateMain.Margin = new Padding( 2, 0, 2, 0 );
				StateMain.AutoSize = true;

				AirSuperiority = new ImageLabel();
				AirSuperiority.Anchor = AnchorStyles.Left;
				AirSuperiority.Font = parent.MainFont;
				AirSuperiority.ForeColor = parent.MainFontColor;
				AirSuperiority.ImageList = ResourceManager.Instance.Equipments;
				AirSuperiority.ImageIndex = (int)ResourceManager.EquipmentContent.CarrierBasedFighter;
				AirSuperiority.Padding = new Padding( 2, 2, 2, 2 );
				AirSuperiority.Margin = new Padding( 2, 0, 2, 0 );
				AirSuperiority.AutoSize = true;

				SearchingAbility = new ImageLabel();
				SearchingAbility.Anchor = AnchorStyles.Left;
				SearchingAbility.Font = parent.MainFont;
				SearchingAbility.ForeColor = parent.MainFontColor;
				SearchingAbility.ImageList = ResourceManager.Instance.Equipments;
				SearchingAbility.ImageIndex = (int)ResourceManager.EquipmentContent.CarrierBasedRecon;
				SearchingAbility.Padding = new Padding( 2, 2, 2, 2 );	//fixme:paddingか何かでアイコンが欠けるバグがあるので要修正
				SearchingAbility.Margin = new Padding( 2, 0, 2, 0 );
				SearchingAbility.AutoSize = true;

				#endregion

			}

			public TableFleetControl( FormFleet parent, TableLayoutPanel table ) 
				: this( parent ) {
				AddToTable( table );
			}

			public void AddToTable( TableLayoutPanel table ) {

				table.Controls.Add( Name, 0, 0 );
				table.Controls.Add( StateMain, 1, 0 );
				table.Controls.Add( AirSuperiority, 2, 0 );
				table.Controls.Add( SearchingAbility, 3, 0 );

				int row = 0;
				#region set RowStyle
				RowStyle rs = new RowStyle( SizeType.Absolute, 21 );

				if ( table.RowStyles.Count > row )
					table.RowStyles[row] = rs;
				else
					while ( table.RowStyles.Count <= row )
						table.RowStyles.Add( rs );
				#endregion
			}

			public void Update( FleetData fleet ) {

				KCDatabase db = KCDatabase.Instance;
					
				Name.Text = fleet.Name;



				#region set StateMain

				//memo: [大破出撃中>出撃中|遠征中|入渠中>大破艦あり>未補給>疲労>中破艦あり>泊地修理中>出撃可能]
				//memo: 泊地修理は工作艦が中破しているとできない、忘れないよう
				//fixme:　あまりにきたないので書き直しを要請する
				//todo: 入渠/遠征：完了時間のツールチップ


				var ships = KCDatabase.Instance.Ships;

				//大破出撃中
				if ( fleet.FleetMember.Count(
						( int id ) => {
							if ( id == -1 ) return false;
							else return (double)ships[id].HPCurrent / ships[id].HPMax <= 0.25;
						}
					) > 0 ) {

					StateMain.Text = "大破艦あり！";


				}
					//undone: 出撃中
				else if ( false ) {


				}
					//遠征中
				else if ( fleet.ExpeditionState != 0 ) {

					StateMain.Text = "遠征中 " + DateConverter.ToTimeRemainString( fleet.ExpeditionTime );

				}
					//入渠艦あり
				else {

					long ntime = KCDatabase.Instance.Docks.Max(
						( KeyValuePair<int, DockData> dock ) => {
							if ( dock.Value.State < 1 ) return 0;
							else if ( fleet.FleetMember.Count( ( int id ) => id == dock.Value.ShipID ) > 0 )
								return dock.Value.CompletionTime.ToBinary();
							else return 0;
						}
						);

					if ( ntime > 0 ) {
						StateMain.Text = "入渠中 " + DateConverter.ToTimeRemainString( DateTime.FromBinary( ntime ) );

					} else if ( fleet.FleetMember.Count(
					   ( int id ) => {
						   if ( id == -1 ) return false;
						   else return (double)ships[id].HPCurrent / ships[id].HPMax <= 0.25;
					   }
					) > 0 ) {

						StateMain.Text = "大破艦あり！";


					} else if ( fleet.FleetMember.Count(
						( int id ) => {
							if ( id == -1 ) return false;
							else return ships[id].Fuel < KCDatabase.Instance.MasterShips[ships[id].ShipID].Fuel ||
								ships[id].Ammo < KCDatabase.Instance.MasterShips[ships[id].ShipID].Ammo;
						}
					) > 0 ) {

						StateMain.Text = "未補給";


					} else if ( fleet.FleetMember.Count(
						( int id ) => {
							if ( id == -1 ) return false;
							else return ships[id].Condition < 40;
						}
					) > 0 ) {

						StateMain.Text = "疲労";


						//undone: 泊地修理中
					} else if ( false ) {

					} else {

						StateMain.Text = "出撃可能！";
					}

				}

				#endregion


				//制空戦力計算
				{
					int airSuperiority = 0;

					for ( int i = 0; i < fleet.FleetMember.Count; i++ ) {

						if ( fleet.FleetMember[i] == -1 )
							continue;

						ShipData ship = db.Ships[fleet.FleetMember[i]];
						for ( int j = 0; j < ship.Slot.Count; j++ ) {

							if ( ship.Slot[j] == -1 )
								continue;

							EquipmentDataMaster eq = db.MasterEquipments[db.Equipments[ship.Slot[j]].EquipmentID];

							switch ( eq.EquipmentType[2] ) {
								case 6:		//艦戦
								case 7:		//艦爆
								case 8:		//艦攻
								case 11:	//水爆
									airSuperiority += (int)( eq.AA * Math.Sqrt( ship.Aircraft[j] ) );
									break;
							}
						}
					}

					AirSuperiority.Text = airSuperiority.ToString();
				}


				//索敵能力計算　(水偵|艦偵)*2 + 電探 + √(その他)　とする
				//この式は正確ではないらしいので参考までに
				{
					int los_reconplane = 0;
					int los_radar = 0;
					int los_other = 0;

					for ( int i = 0; i < fleet.FleetMember.Count; i++ ) {

						if ( fleet.FleetMember[i] == -1 )
							continue;

						ShipData ship = db.Ships[fleet.FleetMember[i]];

						los_other += ship.LOSBase;

						for ( int j = 0; j < ship.Slot.Count; j++ ) {

							if ( ship.Slot[j] == -1 )
								continue;

							EquipmentDataMaster eq = db.MasterEquipments[db.Equipments[ship.Slot[j]].EquipmentID];

							switch ( eq.EquipmentType[2] ) {
								case 9:		//艦偵
								case 10:	//水偵
								case 11:	//水爆
									if ( ship.Aircraft[j] > 0 )
										los_reconplane += eq.LOS * 2;
									break;

								case 12:	//小型電探
								case 13:	//大型電探
									los_radar += eq.LOS;
									break;

								default:
									los_other += eq.LOS;
									break;
							}
						}
					}

					SearchingAbility.Text = ( (int)Math.Sqrt( los_other ) + los_radar + los_reconplane ).ToString();

				}

			}


			public void Refresh() {

			}

		}

		private class TableMemberControl {
			public Label Name;
			public ShipStatusLevel Level;
			public ShipStatusHP HP;
			public ImageLabel Condition;
			public ShipStatusResource ShipResource;
			

			public TableMemberControl( FormFleet parent ) {

				#region Initialize

				Name = new Label();
				Name.SuspendLayout();
				Name.Text = "*nothing*";
				Name.Anchor = AnchorStyles.Left;
				Name.Font = parent.MainFont;
				Name.ForeColor = parent.MainFontColor;
				Name.Padding = new Padding( 0, 1, 0, 1 );
				Name.Margin = new Padding( 2, 0, 2, 0 );
				Name.AutoSize = true;
				Name.Visible = false;
				Name.ResumeLayout();

				Level = new ShipStatusLevel();
				Level.SuspendLayout();
				Level.Anchor = AnchorStyles.Left;
				Level.Value = 0;
				Level.MaximumValue = 150;
				Level.ValueNext = 0;
				Level.MainFont = parent.MainFont;
				Level.SubFont = parent.SubFont;
				Level.MainFontColor = parent.MainFontColor;
				Level.SubFontColor = parent.SubFontColor;
				//Level.TextNext = "n.";
				Level.Padding = new Padding( 0, 0, 0, 0 );
				Level.Margin = new Padding( 2, 0, 2, 0 );
				Level.AutoSize = true;
				Level.Visible = false;
				Name.ResumeLayout();

				HP = new ShipStatusHP();
				HP.SuspendLayout();
				HP.Anchor = AnchorStyles.Left;
				HP.Value = 0;
				HP.MaximumValue = 0;
				HP.MaximumDigit = 999;
				HP.MainFont = parent.MainFont;
				HP.SubFont = parent.SubFont;
				HP.MainFontColor = parent.MainFontColor;
				HP.SubFontColor = parent.SubFontColor;
				HP.Padding = new Padding( 0, 0, 0, 0 );
				HP.Margin = new Padding( 2, 1, 2, 2 );
				HP.AutoSize = true;
				HP.Visible = false;
				HP.ResumeLayout();

				Condition = new ImageLabel();
				Condition.SuspendLayout();
				Condition.Text = "*";
				Condition.Anchor = AnchorStyles.Right;
				Condition.Font = parent.MainFont;
				Condition.ForeColor = parent.MainFontColor;
				Condition.TextAlign = ContentAlignment.BottomLeft;
				Condition.ImageList = ResourceManager.Instance.Icons;
				Condition.Padding = new Padding( 2, 2, 2, 2 );
				Condition.Margin = new Padding( 2, 0, 2, 0 );
				Condition.Size = new Size( 40, 20 );
				Condition.AutoSize = true;
				Condition.Visible = false;
				Condition.ResumeLayout();

				ShipResource = new ShipStatusResource( parent.ToolTipInfo );
				ShipResource.SuspendLayout();
				ShipResource.FuelCurrent = 0;
				ShipResource.FuelMax = 0;
				ShipResource.AmmoCurrent = 0;
				ShipResource.AmmoMax = 0;
				ShipResource.Anchor = AnchorStyles.Left;
				ShipResource.Padding = new Padding( 0, 2, 0, 1 );
				ShipResource.Margin = new Padding( 2, 0, 2, 0 );
				ShipResource.Size = new Size( 40, 20 );
				ShipResource.AutoSize = false;
				ShipResource.Visible = false;
				ShipResource.ResumeLayout();

				#endregion

			}

			public TableMemberControl( FormFleet parent, TableLayoutPanel table, int row )
				: this( parent ) {
				AddToTable( table, row );
			}


			public void AddToTable( TableLayoutPanel table, int row ) {

				table.Controls.Add( Name, 0, row );
				table.Controls.Add( Level, 1, row );
				table.Controls.Add( HP, 2, row );
				table.Controls.Add( Condition, 3, row );
				table.Controls.Add( ShipResource, 4, row );

				#region set RowStyle
				RowStyle rs = new RowStyle( SizeType.Absolute, 21 );

				if ( table.RowStyles.Count > row )
					table.RowStyles[row] = rs;
				else
					while ( table.RowStyles.Count <= row )
						table.RowStyles.Add( rs );
				#endregion
			}

			public void Update( int shipMasterID ) {

				KCDatabase db = KCDatabase.Instance;

				if ( shipMasterID != -1 ) {

					ShipData ship = db.Ships[shipMasterID];
					ShipDataMaster shipmaster = db.MasterShips[ship.ShipID];


					Name.Text = shipmaster.Name;
					
					Level.Value = ship.Level;
					Level.ValueNext = ship.ExpNext;
					
					HP.Value = ship.HPCurrent;
					HP.MaximumValue = ship.HPMax;
					HP.RepairTime = null;
					foreach ( var dock in db.Docks ) {
						if ( dock.Value.ShipID == shipMasterID ) {
							HP.RepairTime = dock.Value.CompletionTime;
							break;
						}
					}
					
					Condition.Text = ship.Condition.ToString();
					if ( ship.Condition < 20 ) {
						Condition.ImageIndex = (int)ResourceManager.IconContent.ConditionVeryTired;
					} else if ( ship.Condition < 30 ) {
						Condition.ImageIndex = (int)ResourceManager.IconContent.ConditionTired;
					} else if ( ship.Condition < 40 ) {
						Condition.ImageIndex = (int)ResourceManager.IconContent.ConditionLittleTired;
					} else if ( ship.Condition < 50 ) {
						Condition.ImageIndex = (int)ResourceManager.IconContent.ConditionNormal;
					} else {
						Condition.ImageIndex = (int)ResourceManager.IconContent.ConditionSparkle;
					}

					ShipResource.SetResources( ship.Fuel, shipmaster.Fuel, ship.Ammo, shipmaster.Ammo );
				
				}


				Name.Visible =
				Level.Visible =
				HP.Visible = 
				Condition.Visible = 
				ShipResource.Visible = shipMasterID != -1;

			}
		}




		public int FleetID { get; private set; }

		
		public Font MainFont { get; set; }
		public Font SubFont { get; set; }
		public Color MainFontColor { get; set; }
		public Color SubFontColor { get; set; }


		private TableFleetControl ControlFleet;
		private TableMemberControl[] ControlMember;
		

		
		public FormFleet( FormMain parent, int fleetID ) {
			InitializeComponent();

			FleetID = fleetID;
			parent.UpdateTimerTick += parent_UpdateTimerTick;

			//todo: 後々外部から設定できるように
			MainFont = new Font( "Meiryo UI", 12, FontStyle.Regular, GraphicsUnit.Pixel );
			SubFont = new Font( "Meiryo UI", 10, FontStyle.Regular, GraphicsUnit.Pixel );
			MainFontColor = Color.FromArgb( 0x00, 0x00, 0x00 );
			SubFontColor = Color.FromArgb( 0x88, 0x88, 0x88 );


			//ui init

			//doublebuffered
			System.Reflection.PropertyInfo prop = typeof( TableLayoutPanel ).GetProperty( "DoubleBuffered", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic );
			prop.SetValue( TableFleet, true, null );
			prop.SetValue( TableMember, true, null );


			TableFleet.SuspendLayout();
			TableFleet.BorderStyle = BorderStyle.FixedSingle;
			ControlFleet = new TableFleetControl( this, TableFleet );
			TableFleet.ResumeLayout();


			TableMember.SuspendLayout();
			ControlMember = new TableMemberControl[6];
			for ( int i = 0; i < ControlMember.Length; i++ ) {
				ControlMember[i] = new TableMemberControl( this, TableMember, i );
			}
			TableMember.ResumeLayout();

		}

		

		private void FormFleet_Load( object sender, EventArgs e ) {

			KCDatabase Database = KCDatabase.Instance;

			Text = string.Format( "#{0}", FleetID );
			
			Database.FleetUpdated += ( DatabaseUpdatedEventArgs e1 ) => Invoke( new KCDatabase.DatabaseUpdatedEventHandler( Database_FleetUpdated ), e1 );
			Database.ShipsUpdated += ( DatabaseUpdatedEventArgs e1 ) => Invoke( new KCDatabase.DatabaseUpdatedEventHandler( Database_FleetUpdated ), e1 );
			Database.DocksUpdated += ( DatabaseUpdatedEventArgs e1 ) => Invoke( new KCDatabase.DatabaseUpdatedEventHandler( Database_FleetUpdated ), e1 );
			
		}


		void Database_FleetUpdated( DatabaseUpdatedEventArgs e ) {

			KCDatabase db = KCDatabase.Instance;
			FleetData fleet = db.Fleet.Fleets[FleetID];

			TableFleet.SuspendLayout();
			ControlFleet.Update( fleet );
			TableFleet.ResumeLayout();

			TableMember.SuspendLayout();
			for ( int i = 0; i < ControlMember.Length; i++ ) {
				ControlMember[i].Update( fleet.FleetMember[i] );
			}
			TableMember.ResumeLayout();

		}


		void parent_UpdateTimerTick( object sender, EventArgs e ) {

			TableFleet.SuspendLayout();
			{
				FleetData fleet = KCDatabase.Instance.Fleet.Fleets[FleetID];
				if ( fleet != null )
					ControlFleet.Update( fleet );		//fixme:わざわざデータを再読み込みする必要はないのでは？タイマだけ保持しておけばいい感
			}
			TableFleet.ResumeLayout();

			TableMember.SuspendLayout();
			for ( int i = 0; i < ControlMember.Length; i++ ) {
				ControlMember[i].HP.Refresh();
			}
			TableMember.ResumeLayout();

		}


		private void TableMember_CellPaint( object sender, TableLayoutCellPaintEventArgs e ) {
			e.Graphics.DrawLine( Pens.Silver, e.CellBounds.X, e.CellBounds.Bottom - 1, e.CellBounds.Right - 1, e.CellBounds.Bottom - 1 );
		}


		

	}

}
