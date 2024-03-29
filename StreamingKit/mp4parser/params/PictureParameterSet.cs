/*
Copyright (c) 2011 Stanislav Vitvitskiy

Permission is hereby granted, free of charge, to any person obtaining a copy of this
software and associated documentation files (the "Software"), to deal in the Software
without restriction, including without limitation the rights to use, copy, modify,
merge, publish, distribute, sublicense, and/or sell copies of the Software, and to
permit persons to whom the Software is furnished to do so, subject to the following
conditions:

The above copyright notice and this permission notice shall be included in all copies or
substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE
OR OTHER DEALINGS IN THE SOFTWARE.
*/
namespace mp4parser.h264.model
{

    using System;
    using System.IO;
    using CAVLCReader = read.CAVLCReader;
    using CAVLCWriter = write.CAVLCWriter;

    /// <summary>
    /// Picture Parameter Set entity of H264 bitstream
    /// <para>
    /// capable to serialize / deserialize with CAVLC bitstream</para>
    /// 
    /// @author Stanislav Vitvitskiy
    /// </summary>
    public class PictureParameterSet
    {

        public class PPSExt
        {
            public bool transform_8x8_mode_flag;
            public ScalingMatrix scalindMatrix = new ScalingMatrix();
            public int second_chroma_qp_index_offset;
            public bool[] pic_scaling_list_present_flag;

            public override string ToString()
            {
                return "PPSExt{" + "transform_8x8_mode_flag=" + transform_8x8_mode_flag + ", scalindMatrix=" + scalindMatrix + ", second_chroma_qp_index_offset=" + second_chroma_qp_index_offset + ", pic_scaling_list_present_flag=" + pic_scaling_list_present_flag + '}';
            }
        }

        public bool entropy_coding_mode_flag { get; set; }
        public int num_ref_idx_l0_active_minus1 { get; set; }
        public int num_ref_idx_l1_active_minus1 { get; set; }
        public int slice_group_change_rate_minus1 { get; set; }
        public int pic_parameter_set_id { get; set; }
        public int seq_parameter_set_id { get; set; }
        public bool pic_order_present_flag { get; set; }
        public int num_slice_groups_minus1 { get; set; }
        public int slice_group_map_type { get; set; }
        public bool weighted_pred_flag { get; set; }
        public int weighted_bipred_idc { get; set; }
        public int pic_init_qp_minus26 { get; set; }
        public int pic_init_qs_minus26 { get; set; }
        public int chroma_qp_index_offset { get; set; }
        public bool deblocking_filter_control_present_flag { get; set; }
        public bool constrained_intra_pred_flag { get; set; }
        public bool redundant_pic_cnt_present_flag { get; set; }
        public int[] top_left { get; set; }
        public int[] bottom_right { get; set; }
        public int[] run_length_minus1 { get; set; }
        public bool slice_group_change_direction_flag { get; set; }
        public int[] slice_group_id { get; set; }
        public PPSExt extended { get; set; }

        public static PictureParameterSet read(byte[] b)
        {
            return read(new MemoryStream(b));
        }

        public static PictureParameterSet read(Stream @is)
        {
            var reader = new CAVLCReader(@is);
            PictureParameterSet pps = new PictureParameterSet
            {
                pic_parameter_set_id = reader.ReadUE("PPS: pic_parameter_set_id"),
                seq_parameter_set_id = reader.ReadUE("PPS: seq_parameter_set_id"),
                entropy_coding_mode_flag = reader.ReadBool("PPS: entropy_coding_mode_flag"),
                pic_order_present_flag = reader.ReadBool("PPS: pic_order_present_flag"),
                num_slice_groups_minus1 = reader.ReadUE("PPS: num_slice_groups_minus1")
            };
            if (pps.num_slice_groups_minus1 > 0)
            {
                pps.slice_group_map_type = reader.ReadUE("PPS: slice_group_map_type");
                pps.top_left = new int[pps.num_slice_groups_minus1 + 1];
                pps.bottom_right = new int[pps.num_slice_groups_minus1 + 1];
                pps.run_length_minus1 = new int[pps.num_slice_groups_minus1 + 1];
                if (pps.slice_group_map_type == 0)
                {
                    for (int iGroup = 0; iGroup <= pps.num_slice_groups_minus1; iGroup++)
                    {
                        pps.run_length_minus1[iGroup] = reader.ReadUE("PPS: run_length_minus1");
                    }
                }
                else if (pps.slice_group_map_type == 2)
                {
                    for (int iGroup = 0; iGroup < pps.num_slice_groups_minus1; iGroup++)
                    {
                        pps.top_left[iGroup] = reader.ReadUE("PPS: top_left");
                        pps.bottom_right[iGroup] = reader.ReadUE("PPS: bottom_right");
                    }
                }
                else if (pps.slice_group_map_type == 3 || pps.slice_group_map_type == 4 || pps.slice_group_map_type == 5)
                {
                    pps.slice_group_change_direction_flag = reader.ReadBool("PPS: slice_group_change_direction_flag");
                    pps.slice_group_change_rate_minus1 = reader.ReadUE("PPS: slice_group_change_rate_minus1");
                }
                else if (pps.slice_group_map_type == 6)
                {
                    int NumberBitsPerSliceGroupId;
                    if (pps.num_slice_groups_minus1 + 1 > 4)
                    {
                        NumberBitsPerSliceGroupId = 3;
                    }
                    else if (pps.num_slice_groups_minus1 + 1 > 2)
                    {
                        NumberBitsPerSliceGroupId = 2;
                    }
                    else
                    {
                        NumberBitsPerSliceGroupId = 1;
                    }
                    int pic_size_in_map_units_minus1 = reader.ReadUE("PPS: pic_size_in_map_units_minus1");
                    pps.slice_group_id = new int[pic_size_in_map_units_minus1 + 1];
                    for (int i = 0; i <= pic_size_in_map_units_minus1; i++)
                    {
                        pps.slice_group_id[i] = reader.ReadU(NumberBitsPerSliceGroupId, "PPS: slice_group_id [" + i + "]f");
                    }
                }
            }
            pps.num_ref_idx_l0_active_minus1 = reader.ReadUE("PPS: num_ref_idx_l0_active_minus1");
            pps.num_ref_idx_l1_active_minus1 = reader.ReadUE("PPS: num_ref_idx_l1_active_minus1");
            pps.weighted_pred_flag = reader.ReadBool("PPS: weighted_pred_flag");
            pps.weighted_bipred_idc = (int)reader.ReadNBit(2, "PPS: weighted_bipred_idc");
            pps.pic_init_qp_minus26 = reader.ReadSE("PPS: pic_init_qp_minus26");
            pps.pic_init_qs_minus26 = reader.ReadSE("PPS: pic_init_qs_minus26");
            pps.chroma_qp_index_offset = reader.ReadSE("PPS: chroma_qp_index_offset");
            pps.deblocking_filter_control_present_flag = reader.ReadBool("PPS: deblocking_filter_control_present_flag");
            pps.constrained_intra_pred_flag = reader.ReadBool("PPS: constrained_intra_pred_flag");
            pps.redundant_pic_cnt_present_flag = reader.ReadBool("PPS: redundant_pic_cnt_present_flag");
            if (reader.moreRBSPData())
            {
                pps.extended = new PictureParameterSet.PPSExt
                {
                    transform_8x8_mode_flag = reader.ReadBool("PPS: transform_8x8_mode_flag")
                };
                bool pic_scaling_matrix_present_flag = reader.ReadBool("PPS: pic_scaling_matrix_present_flag");
                if (pic_scaling_matrix_present_flag)
                {
                    for (int i = 0; i < 6 + 2 * (pps.extended.transform_8x8_mode_flag ? 1 : 0); i++)
                    {
                        bool pic_scaling_list_present_flag = reader.ReadBool("PPS: pic_scaling_list_present_flag");
                        if (pic_scaling_list_present_flag)
                        {
                            pps.extended.scalindMatrix.ScalingList4x4 = new ScalingList[8];
                            pps.extended.scalindMatrix.ScalingList8x8 = new ScalingList[8];
                            if (i < 6)
                            {
                                pps.extended.scalindMatrix.ScalingList4x4[i] = ScalingList.read(reader, 16);
                            }
                            else
                            {
                                pps.extended.scalindMatrix.ScalingList8x8[i - 6] = ScalingList.read(reader, 64);
                            }
                        }
                    }
                }
                pps.extended.second_chroma_qp_index_offset = reader.ReadSE("PPS: second_chroma_qp_index_offset");
            }

            reader.ReadTrailingBits();

            return pps;
        }

        public void write(Stream @out)
        {
            CAVLCWriter writer = new CAVLCWriter(@out);

            writer.writeUE(pic_parameter_set_id, "PPS: pic_parameter_set_id");
            writer.writeUE(seq_parameter_set_id, "PPS: seq_parameter_set_id");
            writer.writeBool(entropy_coding_mode_flag, "PPS: entropy_coding_mode_flag");
            writer.writeBool(pic_order_present_flag, "PPS: pic_order_present_flag");
            writer.writeUE(num_slice_groups_minus1, "PPS: num_slice_groups_minus1");
            if (num_slice_groups_minus1 > 0)
            {
                writer.writeUE(slice_group_map_type, "PPS: slice_group_map_type");
                int[] top_left = new int[1];
                int[] bottom_right = new int[1];
                int[] run_length_minus1 = new int[1];
                if (slice_group_map_type == 0)
                {
                    for (int iGroup = 0; iGroup <= num_slice_groups_minus1; iGroup++)
                    {
                        writer.writeUE(run_length_minus1[iGroup], "PPS: ");
                    }
                }
                else if (slice_group_map_type == 2)
                {
                    for (int iGroup = 0; iGroup < num_slice_groups_minus1; iGroup++)
                    {
                        writer.writeUE(top_left[iGroup], "PPS: ");
                        writer.writeUE(bottom_right[iGroup], "PPS: ");
                    }
                }
                else if (slice_group_map_type == 3 || slice_group_map_type == 4 || slice_group_map_type == 5)
                {
                    writer.writeBool(slice_group_change_direction_flag, "PPS: slice_group_change_direction_flag");
                    writer.writeUE(slice_group_change_rate_minus1, "PPS: slice_group_change_rate_minus1");
                }
                else if (slice_group_map_type == 6)
                {
                    int NumberBitsPerSliceGroupId;
                    if (num_slice_groups_minus1 + 1 > 4)
                    {
                        NumberBitsPerSliceGroupId = 3;
                    }
                    else if (num_slice_groups_minus1 + 1 > 2)
                    {
                        NumberBitsPerSliceGroupId = 2;
                    }
                    else
                    {
                        NumberBitsPerSliceGroupId = 1;
                    }
                    writer.writeUE(slice_group_id.Length, "PPS: ");
                    for (int i = 0; i <= slice_group_id.Length; i++)
                    {
                        writer.writeU(slice_group_id[i], NumberBitsPerSliceGroupId);
                    }
                }
            }
            writer.writeUE(num_ref_idx_l0_active_minus1, "PPS: num_ref_idx_l0_active_minus1");
            writer.writeUE(num_ref_idx_l1_active_minus1, "PPS: num_ref_idx_l1_active_minus1");
            writer.writeBool(weighted_pred_flag, "PPS: weighted_pred_flag");
            writer.writeNBit(weighted_bipred_idc, 2, "PPS: weighted_bipred_idc");
            writer.writeSE(pic_init_qp_minus26, "PPS: pic_init_qp_minus26");
            writer.writeSE(pic_init_qs_minus26, "PPS: pic_init_qs_minus26");
            writer.writeSE(chroma_qp_index_offset, "PPS: chroma_qp_index_offset");
            writer.writeBool(deblocking_filter_control_present_flag, "PPS: deblocking_filter_control_present_flag");
            writer.writeBool(constrained_intra_pred_flag, "PPS: constrained_intra_pred_flag");
            writer.writeBool(redundant_pic_cnt_present_flag, "PPS: redundant_pic_cnt_present_flag");
            if (extended != null)
            {
                writer.writeBool(extended.transform_8x8_mode_flag, "PPS: transform_8x8_mode_flag");
                writer.writeBool(extended.scalindMatrix != null, "PPS: scalindMatrix");
                if (extended.scalindMatrix != null)
                {
                    for (int i = 0; i < 6 + 2 * (extended.transform_8x8_mode_flag ? 1 : 0); i++)
                    {
                        if (i < 6)
                        {
                            writer.writeBool(extended.scalindMatrix.ScalingList4x4[i] != null, "PPS: ");
                            if (extended.scalindMatrix.ScalingList4x4[i] != null)
                            {
                                extended.scalindMatrix.ScalingList4x4[i].write(writer);
                            }

                        }
                        else
                        {
                            writer.writeBool(extended.scalindMatrix.ScalingList8x8[i - 6] != null, "PPS: ");
                            if (extended.scalindMatrix.ScalingList8x8[i - 6] != null)
                            {
                                extended.scalindMatrix.ScalingList8x8[i - 6].write(writer);
                            }
                        }
                    }
                }
                writer.writeSE(extended.second_chroma_qp_index_offset, "PPS: ");
            }

            writer.writeTrailingBits();
        }

        public override int GetHashCode()
        {
            const int prime = 31;
            int result = 1;
            result = prime * result + bottom_right.GetHashCode();
            result = prime * result + chroma_qp_index_offset;
            result = prime * result + (constrained_intra_pred_flag ? 1231 : 1237);
            result = prime * result + (deblocking_filter_control_present_flag ? 1231 : 1237);
            result = prime * result + (entropy_coding_mode_flag ? 1231 : 1237);
            result = prime * result + ((extended == null) ? 0 : extended.GetHashCode());
            result = prime * result + num_ref_idx_l0_active_minus1;
            result = prime * result + num_ref_idx_l1_active_minus1;
            result = prime * result + num_slice_groups_minus1;
            result = prime * result + pic_init_qp_minus26;
            result = prime * result + pic_init_qs_minus26;
            result = prime * result + (pic_order_present_flag ? 1231 : 1237);
            result = prime * result + pic_parameter_set_id;
            result = prime * result + (redundant_pic_cnt_present_flag ? 1231 : 1237);
            result = prime * result + run_length_minus1.GetHashCode();
            result = prime * result + seq_parameter_set_id;
            result = prime * result + (slice_group_change_direction_flag ? 1231 : 1237);
            result = prime * result + slice_group_change_rate_minus1;
            result = prime * result + slice_group_id.GetHashCode();
            result = prime * result + slice_group_map_type;
            result = prime * result + top_left.GetHashCode();
            result = prime * result + weighted_bipred_idc;
            result = prime * result + (weighted_pred_flag ? 1231 : 1237);

            return result;
        }

        public override bool Equals(object obj)
        {
            if (this == obj)
            {
                return true;
            }
            if (obj == null)
            {
                return false;
            }
            if (this.GetType() != obj.GetType())
            {
                return false;
            }
            PictureParameterSet other = (PictureParameterSet)obj;

            if (!Array.Equals(bottom_right, other.bottom_right))
            {
                return false;
            }
            if (chroma_qp_index_offset != other.chroma_qp_index_offset)
            {
                return false;
            }
            if (constrained_intra_pred_flag != other.constrained_intra_pred_flag)
            {
                return false;
            }
            if (deblocking_filter_control_present_flag != other.deblocking_filter_control_present_flag)
            {
                return false;
            }
            if (entropy_coding_mode_flag != other.entropy_coding_mode_flag)
            {
                return false;
            }
            if (extended == null)
            {
                if (other.extended != null)
                {
                    return false;
                }
            }
            else if (!extended.Equals(other.extended))
            {
                return false;
            }
            if (num_ref_idx_l0_active_minus1 != other.num_ref_idx_l0_active_minus1)
            {
                return false;
            }
            if (num_ref_idx_l1_active_minus1 != other.num_ref_idx_l1_active_minus1)
            {
                return false;
            }
            if (num_slice_groups_minus1 != other.num_slice_groups_minus1)
            {
                return false;
            }
            if (pic_init_qp_minus26 != other.pic_init_qp_minus26)
            {
                return false;
            }
            if (pic_init_qs_minus26 != other.pic_init_qs_minus26)
            {
                return false;
            }
            if (pic_order_present_flag != other.pic_order_present_flag)
            {
                return false;
            }
            if (pic_parameter_set_id != other.pic_parameter_set_id)
            {
                return false;
            }
            if (redundant_pic_cnt_present_flag != other.redundant_pic_cnt_present_flag)
            {
                return false;
            }
            if (!Array.Equals(run_length_minus1, other.run_length_minus1))
            {
                return false;
            }
            if (seq_parameter_set_id != other.seq_parameter_set_id)
            {
                return false;
            }
            if (slice_group_change_direction_flag != other.slice_group_change_direction_flag)
            {
                return false;
            }
            if (slice_group_change_rate_minus1 != other.slice_group_change_rate_minus1)
            {
                return false;
            }
            if (!Array.Equals(slice_group_id, other.slice_group_id))
            {
                return false;
            }
            if (slice_group_map_type != other.slice_group_map_type)
            {
                return false;
            }
            if (!Array.Equals(top_left, other.top_left))
            {
                return false;
            }
            if (weighted_bipred_idc != other.weighted_bipred_idc)
            {
                return false;
            }
            if (weighted_pred_flag != other.weighted_pred_flag)
            {
                return false;
            }
            return true;
        }

        public override string ToString()
        {
            return "PictureParameterSet{" + "\n       entropy_coding_mode_flag=" + entropy_coding_mode_flag + ",\n       num_ref_idx_l0_active_minus1=" + num_ref_idx_l0_active_minus1 + ",\n       num_ref_idx_l1_active_minus1=" + num_ref_idx_l1_active_minus1 + ",\n       slice_group_change_rate_minus1=" + slice_group_change_rate_minus1 + ",\n       pic_parameter_set_id=" + pic_parameter_set_id + ",\n       seq_parameter_set_id=" + seq_parameter_set_id + ",\n       pic_order_present_flag=" + pic_order_present_flag + ",\n       num_slice_groups_minus1=" + num_slice_groups_minus1 + ",\n       slice_group_map_type=" + slice_group_map_type + ",\n       weighted_pred_flag=" + weighted_pred_flag + ",\n       weighted_bipred_idc=" + weighted_bipred_idc + ",\n       pic_init_qp_minus26=" + pic_init_qp_minus26 + ",\n       pic_init_qs_minus26=" + pic_init_qs_minus26 + ",\n       chroma_qp_index_offset=" + chroma_qp_index_offset + ",\n       deblocking_filter_control_present_flag=" + deblocking_filter_control_present_flag + ",\n       constrained_intra_pred_flag=" + constrained_intra_pred_flag + ",\n       redundant_pic_cnt_present_flag=" + redundant_pic_cnt_present_flag + ",\n       top_left=" + top_left + ",\n       bottom_right=" + bottom_right + ",\n       run_length_minus1=" + run_length_minus1 + ",\n       slice_group_change_direction_flag=" + slice_group_change_direction_flag + ",\n       slice_group_id=" + slice_group_id + ",\n       extended=" + extended + '}';
        }
    }

}