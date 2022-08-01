using System.IO;
using mp4parser.h264.model;
using mp4parser.h264.read;
public class SliceHeader
{

    public enum SliceType { P, B, I, SP, SI }

    public int first_mb_in_slice;
    public SliceType slice_type;
    public int pic_parameter_set_id;
    public int colour_plane_id;
    public int frame_num;
    public bool field_pic_flag = false;
    public bool bottom_field_flag = false;
    public int idr_pic_id;
    public int pic_order_cnt_lsb;
    public int delta_pic_order_cnt_bottom;

    public SliceHeader(Stream @is, SeqParameterSet sps, PictureParameterSet pps, bool IdrPicFlag)
    {
        @is.ReadByte();
        var reader = new CAVLCReader(@is);
        first_mb_in_slice = reader.ReadUE("SliceHeader: first_mb_in_slice");
        switch (reader.ReadUE("SliceHeader: slice_type"))
        {
            case 0:
            case 5:
                slice_type = SliceType.P;
                break;

            case 1:
            case 6:
                slice_type = SliceType.B;
                break;

            case 2:
            case 7:
                slice_type = SliceType.I;
                break;

            case 3:
            case 8:
                slice_type = SliceType.SP;
                break;

            case 4:
            case 9:
                slice_type = SliceType.SI;
                break;

        }
        pic_parameter_set_id = reader.ReadUE("SliceHeader: pic_parameter_set_id");
        if (sps.residual_color_transform_flag)
        {
            colour_plane_id = reader.ReadU(2, "SliceHeader: colour_plane_id");
        }
        frame_num = reader.ReadU(sps.log2_max_frame_num_minus4 + 4, "SliceHeader: frame_num");

        if (!sps.frame_mbs_only_flag)
        {
            field_pic_flag = reader.ReadBool("SliceHeader: field_pic_flag");
            if (field_pic_flag)
            {
                bottom_field_flag = reader.ReadBool("SliceHeader: bottom_field_flag");
            }
        }
        if (IdrPicFlag)
        {
            idr_pic_id = reader.ReadUE("SliceHeader: idr_pic_id");
            if (sps.pic_order_cnt_type == 0)
            {
                pic_order_cnt_lsb = reader.ReadU(sps.log2_max_pic_order_cnt_lsb_minus4 + 4, "SliceHeader: pic_order_cnt_lsb");
                if (pps.pic_order_present_flag && !field_pic_flag)
                {
                    delta_pic_order_cnt_bottom = reader.ReadSE("SliceHeader: delta_pic_order_cnt_bottom");
                }
            }
        }

    }
    public SliceHeader(Stream @is)
    {
        @is.ReadByte();
        var reader = new CAVLCReader(@is);
        first_mb_in_slice = reader.ReadUE("SliceHeader: first_mb_in_slice");
        switch (reader.ReadUE("SliceHeader: slice_type"))
        {
            case 0:
            case 5:
                slice_type = SliceType.P;
                break;
            case 1:
            case 6:
                slice_type = SliceType.B;
                break;
            case 2:
            case 7:
                slice_type = SliceType.I;
                break;
            case 3:
            case 8:
                slice_type = SliceType.SP;
                break;
            case 4:
            case 9:
                slice_type = SliceType.SI;
                break;
        }
    }
    public static byte[][] GetSPS_PPS(byte[] enc)
    {
        int i = 4, sps_len = 0, pps_len = 0;
        while (i < enc.Length - 4)
        {
            if (enc[i] == 0 && enc[i + 1] == 0 && enc[i + 2] == 0 && enc[i + 3] == 1)
            {
                sps_len = i;
                break;
            }
            i++;
        }
        i += 1;
        while (i < enc.Length - 4)
        {
            if (enc[i] == 0 && enc[i + 1] == 0 && enc[i + 2] == 0 && enc[i + 3] == 1)
            {
                pps_len = i - sps_len;
                break;
            }
            i++;
        }
        sps_len -= 4;
        pps_len -= 4;
        byte[] sps = new byte[sps_len];
        byte[] pps = new byte[pps_len];

        System.Array.Copy(enc, 4, sps, 0, sps_len);
        System.Array.Copy(enc, sps_len + 4 + 4, pps, 0, pps_len);


        return new byte[][] { sps, pps };
    }
    public override string ToString()
    {
        return "SliceHeader{" + "first_mb_in_slice=" + first_mb_in_slice + ", slice_type=" + slice_type + ", pic_parameter_set_id=" + pic_parameter_set_id + ", colour_plane_id=" + colour_plane_id + ", frame_num=" + frame_num + ", field_pic_flag=" + field_pic_flag + ", bottom_field_flag=" + bottom_field_flag + ", idr_pic_id=" + idr_pic_id + ", pic_order_cnt_lsb=" + pic_order_cnt_lsb + ", delta_pic_order_cnt_bottom=" + delta_pic_order_cnt_bottom + '}';
    }
}
