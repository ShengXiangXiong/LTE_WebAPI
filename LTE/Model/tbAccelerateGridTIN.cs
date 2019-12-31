namespace LTE.Model
{
	#region tbAccelerateGridTIN
	/// <summary>
	/// 数据库 [NJCover3D] 中表 [dbo.tbAccelerateGridTIN] 的实体类.
	/// </summary>
	/// 创 建 人: {在这里添加创建人}
	/// 创建日期: 2019/9/2
	/// 修 改 人:
	/// 修改日期:
	/// 修改内容:
	/// 版    本: 1.0.0
	using System;
    using System.Collections.Generic;

	public partial class tbAccelerateGridTIN
	{
		// Instantiate empty tbAccelerateGridTIN for inserting
		public tbAccelerateGridTIN() {}

		public override string ToString()
		{
			return "GXID:" + GXID + " GYID:" + GYID + " GZID:" + GZID + " TINID:" + TINID;
		}

		#region Public Properties
		public int? GXID { get; set; }

		public int? GYID { get; set; }

		public int? GZID { get; set; }

		public int? TINID { get; set; }
		#endregion
	}
	#endregion
}
