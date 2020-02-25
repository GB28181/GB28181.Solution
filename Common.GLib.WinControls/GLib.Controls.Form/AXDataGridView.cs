using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace GLib.Controls.Form
{
	public class AXDataGridView : DataGridView
	{
		private int _index = 0;

		private bool _isScorll = true;

		private int _move = 30;

		private IContainer components = null;

		private Timer timer1;

		[Browsable(true)]
		[DefaultValue(true)]
		public new bool ReadOnly
		{
			get
			{
				return base.ReadOnly;
			}
			set
			{
				base.ReadOnly = value;
			}
		}

		public AXDataGridView()
		{
			InitializeComponent();
			Init();
		}

		private void Init()
		{
			base.AllowUserToAddRows = false;
			base.AllowUserToDeleteRows = false;
			base.BorderStyle = BorderStyle.None;
			base.BackgroundColor = SystemColors.Window;
			base.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
			base.EnableHeadersVisualStyles = false;
			base.RowHeadersVisible = false;
			base.RowTemplate.Height = 20;
			base.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
			base.AutoGenerateColumns = false;
			base.AllowUserToResizeRows = false;
			SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.DoubleBuffer | ControlStyles.OptimizedDoubleBuffer, value: true);
			UpdateStyles();
		}

		private void AXDataGridView_MouseWheel(object sender, MouseEventArgs e)
		{
			_move = 0;
		}

		private void AXDataGridView_RowsAdded(object sender, DataGridViewRowsAddedEventArgs e)
		{
			if (_isScorll && base.FirstDisplayedScrollingRowIndex != -1)
			{
				_index++;
				base.FirstDisplayedScrollingRowIndex = ((base.Rows.Count > 0) ? (base.Rows.Count - 1) : 0);
			}
		}

		private void AXDataGridView_RowsRemoved(object sender, DataGridViewRowsRemovedEventArgs e)
		{
			_index--;
			if (_isScorll && base.FirstDisplayedScrollingRowIndex != -1)
			{
				base.FirstDisplayedScrollingRowIndex = base.Rows.Count - 2;
			}
		}

		private void AXDataGridView_Scroll(object sender, ScrollEventArgs e)
		{
			if (_index > e.NewValue)
			{
				_move = 0;
				_isScorll = false;
			}
			else
			{
				_isScorll = true;
			}
			if (_isScorll)
			{
				_index = e.NewValue;
			}
		}

		private void AXDataGridView_MouseClick(object sender, MouseEventArgs e)
		{
			_isScorll = false;
		}

		private void AXDataGridView_MouseMove(object sender, MouseEventArgs e)
		{
			_move = 0;
		}

		private void timer1_Tick(object sender, EventArgs e)
		{
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing && components != null)
			{
				timer1.Enabled = false;
				timer1.Dispose();
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		private void InitializeComponent()
		{
			components = new System.ComponentModel.Container();
			timer1 = new System.Windows.Forms.Timer(components);
			((System.ComponentModel.ISupportInitialize)this).BeginInit();
			SuspendLayout();
			timer1.Enabled = true;
			timer1.Interval = 1000;
			timer1.Tick += new System.EventHandler(timer1_Tick);
			base.RowTemplate.Height = 23;
			((System.ComponentModel.ISupportInitialize)this).EndInit();
			ResumeLayout(false);
		}
	}
}
