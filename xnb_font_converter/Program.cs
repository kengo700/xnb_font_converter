using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Collections;

namespace xnb_font_converter
{
    class Program
    {
        static int Main(string[] args)
        {

            Console.WriteLine();
            Console.WriteLine("xnb font converter");
            Console.WriteLine();

            string input_file = "";
            string output_file = "";

            int setting_line_spacing = 0;
            int setting_font_size = 0;
            bool has_setting_line_spacing = false;
            bool has_setting_font_size = false;

            if ( args.Length < 2)
            {
                Console.WriteLine("Error: invalid argument");
                Console.WriteLine("Usage: xnb_font_converter.exe <input xnb file> <output xnb file> (font size) (line spacing)");
                return -1;

            } else { 

                input_file = args[0];
                output_file = args[1];

                if (args.Length >= 3)
                {
                    try
                    {
                        setting_font_size = int.Parse(args[2]);
                        has_setting_font_size = true;
                    }
                    catch (Exception e)
                    {
                        // エラー処理
                        Console.Write(e.Message + "\n");
                        Console.WriteLine("Usage: xnb_font_converter.exe <input xnb file> <output xnb file> (font size) (line spacing)");
                        return -1;
                    }

                }

                if (args.Length >= 4)
                {
                    try
                    {
                        setting_line_spacing = int.Parse(args[3]);
                        has_setting_line_spacing = true;
                    }
                    catch (Exception e)
                    {
                        // エラー処理
                        Console.Write(e.Message + "\n");
                        Console.WriteLine("Usage: xnb_font_converter.exe <input xnb file> <output xnb file> (font size) (line spacing)");
                        return -1;
                    }
                }

            }


            // ファイル読み込み
            FileStream fs = null;
            byte[] bs;
            try
            {   
                // https://dobon.net/vb/dotnet/file/filestream.html
                fs = new FileStream(
                    input_file,
                    System.IO.FileMode.Open,
                    System.IO.FileAccess.Read);

                // ファイルを読み込むバイト型配列を作成する
                bs = new byte[fs.Length];

                // ファイルの内容をすべて読み込む
                fs.Read(bs, 0, bs.Length);
            }
            catch (Exception e)
            {
                // エラー処理
                Console.Write(e.Message + "\n");
                return -1;
            }
            finally
            {
                // 後処理
                if (fs != null)
                    fs.Close();
            }


            // ファイルの先頭部分を読み出し
            byte[] original_header = new byte[10];
            for (int i = 0; i < original_header.Length; i++)
            {
                original_header[i] = bs[i];
            }


            // ファイルフォーマットチェック
            if (original_header[0] != 'X' || original_header[1] != 'N' || original_header[2] != 'B')
            {
                Console.WriteLine("Error: invalid file type");
                return -1;
            }

            // ファイルバージョンチェック
            if (original_header[4] != 0x05)
            {
                Console.WriteLine("Error: invalid file version");
                return -1;
            }

            // ファイル圧縮チェック
            if (original_header[5] != 0x00)
            {
                Console.WriteLine("Error: invalid file compressional state");
                return -1;
            }



            // ファイル情報部分を読み進める
            long address_fileinfo_num = Convert.ToInt64("0x0A", 16); // ファイル情報の個数情報位置（間違ってる可能性あり）
            int fileinfo_num_byte = CountByte(bs, (int)address_fileinfo_num);
            int fileinfo_num = Byte2Int(bs, (int)address_fileinfo_num);

            int address_text_size = (int)address_fileinfo_num + fileinfo_num_byte;
            for (int count = 0; count < fileinfo_num; count++)
            {

                int text_size_byte = CountByte(bs, (int)address_text_size);
                int text_size = Byte2Int(bs, address_text_size);

                // 次のテキストサイズ位置
                address_text_size += text_size_byte + text_size + 4;

            }


            // ファイルのフォントテクスチャ部分を読み出し
            int address_texture = address_text_size + 3;
            int address_texture_size = address_texture + 16;
            int texture_size = BitConverter.ToInt32(bs, address_texture_size);
            byte[] original_texture = new byte[16 + 4 + texture_size];

            for (int i = 0; i < original_texture.Length; i++)
            {
                original_texture[i] = bs[address_texture + i];
            }


            // ファイルのグリフ部分を読み出し
            int address_glyphs_size = address_texture + original_texture.Length + 1;
            int address_glyphs = address_glyphs_size + 4;
            int glyphs_num = BitConverter.ToInt32(bs, address_glyphs_size);
            byte[] original_glyphs = new byte[glyphs_num * 16];

            for (int i = 0; i < original_glyphs.Length; i++)
            {
                original_glyphs[i] = bs[address_glyphs + i];
            }


            // ファイルのクロッピング部分を読み出し
            int address_cropping = address_glyphs + original_glyphs.Length + 1 + 4;
            byte[] original_cropping = new byte[glyphs_num * 16];

            for (int i = 0; i < original_cropping.Length; i++)
            {
                original_cropping[i] = bs[address_cropping + i];
            }


            // 文字一覧部分のバイト数をカウント
            int address_char_map = address_cropping + original_cropping.Length + 1 + 4;
            int address_char = address_char_map;
            int char_map_byte_count = 0;

            for (int i = 0; i < glyphs_num; i++)
            {
                int utf8_byte_num = CountUTF8Byte(bs[address_char]);

                // 次の文字の先頭バイト位置
                address_char += utf8_byte_num;
                char_map_byte_count += utf8_byte_num;
            }


            // ファイルの文字一覧部分を読み出し
            byte[] original_char_map = new byte[char_map_byte_count];
            for (int i = 0; i < original_char_map.Length; i++)
            {
                original_char_map[i] = bs[address_char_map + i];
            }


            // ファイルのフォントパラメータ部分を読みだし
            int address_other_data = address_char_map + char_map_byte_count;
            byte[] original_other_data = new byte[8];
            for (int i = 0; i < original_other_data.Length; i++)
            {
                original_other_data[i] = bs[address_other_data + i];
            }


            // ファイルのカーニング部分を読み出し
            int address_kerning = address_other_data + original_other_data.Length + 1 + 4;
            byte[] original_kerning = new byte[glyphs_num * 12];

            for (int i = 0; i < original_kerning.Length; i++)
            {
                original_kerning[i] = bs[address_kerning + i];
            }


            // ファイルのデフォルト記号部分（最後まで）を読み出し
            int address_default_char = address_kerning + original_kerning.Length;
            byte[] original_default_char = new byte[bs.Length - address_default_char];

            for (int i = 0; i < original_default_char.Length; i++)
            {
                original_default_char[i] = bs[address_default_char + i];
            }



            // バージョン6のデータを生成
            List<byte> output_data = new List<byte>();


            // ヘッダデータをコピー
            for (int i = 0; i < original_header.Length; i++)
            {
                output_data.Add(original_header[i]);
            }


            // フォントテクスチャデータをコピー
            for (int i = 0; i < original_texture.Length; i++)
            {
                output_data.Add(original_texture[i]);
            }


            // データ数を挿入
            byte[] glyphs_num_bytes = new byte[CountInt(glyphs_num)];
            glyphs_num_bytes = Int2Byte(glyphs_num);
            for (int i = 0; i < glyphs_num_bytes.Length; i++)
            {
                output_data.Add(glyphs_num_bytes[i]);
            }

            // ファイルのグリフデータをコピー
            for (int i = 0; i < original_glyphs.Length; i++)
            {
                output_data.Add(original_glyphs[i]);
            }


            // データ数を挿入
            for (int i = 0; i < glyphs_num_bytes.Length; i++)
            {
                output_data.Add(glyphs_num_bytes[i]);
            }

            // ファイルのクロッピングデータをコピー
            for (int i = 0; i < original_cropping.Length; i++)
            {
                output_data.Add(original_cropping[i]);
            }


            // データ数を挿入
            for (int i = 0; i < glyphs_num_bytes.Length; i++)
            {
                output_data.Add(glyphs_num_bytes[i]);
            }

            // ファイルの文字一覧データをコピー
            for (int i = 0; i < original_char_map.Length; i++)
            {
                output_data.Add(original_char_map[i]);
            }


            // フォントファイルのパラメータ設定
            //   行間隔データは、文字サイズデータと同じにしておく
            List<byte> output_other_data = new List<byte>();
            for (int i = 0; i < original_other_data.Length; i++)
            {
                output_other_data.Add(original_other_data[i]);
            }
            for (int i = 0; i < 4; i++)
            {
                output_other_data.Add(original_other_data[i]);
            }

            // 行間情報を差し替え（テスト中）
            if (has_setting_line_spacing)
            {
                byte[] output_line_spacing_byte = BitConverter.GetBytes(setting_line_spacing);

                for (int i = 0; i < output_line_spacing_byte.Length; i++)
                {
                    output_other_data[i] = output_line_spacing_byte[i];
                }
            }

            // サイズ情報を差し替え（テスト中）
            if (has_setting_font_size)
            {
                byte[] output_font_size_byte = BitConverter.GetBytes(setting_font_size);

                for (int i = 0; i < output_font_size_byte.Length; i++)
                {
                    output_other_data[8 + i] = output_font_size_byte[i];
                }
            }

            // フォントファイルのパラメータをコピー
            for (int i = 0; i < output_other_data.Count(); i++)
            {
                output_data.Add(output_other_data[i]);
            }


            // データ数を挿入
            for (int i = 0; i < glyphs_num_bytes.Length; i++)
            {
                output_data.Add(glyphs_num_bytes[i]);
            }

            // ファイルのカーニングデータをコピー
            for (int i = 0; i < original_kerning.Length; i++)
            {
                output_data.Add(original_kerning[i]);
            }


            // ファイルのデフォルト記号データ（最後まで）をコピー
            for (int i = 0; i < original_default_char.Length; i++)
            {
                output_data.Add(original_default_char[i]);
            }


            // ファイルサイズデータを更新
            byte[] output_size_byte = BitConverter.GetBytes(output_data.Count());
            for (int i = 0; i < output_size_byte.Length; i++)
            {
                output_data[6 + i] = output_size_byte[i];
            }

            // プラットフォームデータを更新
            output_data[3] = 0x77;

            // バージョンデータを更新
            output_data[4] = 0x06;



            // ファイルに出力
            byte[] b_output_data = output_data.ToArray();
            FileStream fs_output = null;
            try
            {

                fs_output = new FileStream(output_file, FileMode.Create);
                fs_output.Write(b_output_data, 0, b_output_data.Length);

            }
            catch (Exception e)
            {
                // エラー処理
                Console.Write(e.Message + "\n");
                return -1;
            }
            finally
            {
                // 後処理
                if (fs_output != null)
                    fs_output.Close();
            }

            Console.WriteLine("done!");
            return 0;

        }

        // 文字の先頭バイトから、UTF8で何バイトの文字か確認する
        static int CountUTF8Byte(byte head_byte) {

            // 1バイト目の先頭が0であれば1バイトの文字
            if (((1 << 7) & head_byte) == 0)
            {
                return 1;
            }
            // 1バイト目の先頭が110であれば2バイトの文字
            if (((1 << 7) & head_byte) != 0 && ((1 << 6) & head_byte) != 0 && ((1 << 5) & head_byte) == 0)
            {
                return 2;
            }
            // 1バイト目の先頭が1110であれば3バイトの文字
            if (((1 << 7) & head_byte) != 0 && ((1 << 6) & head_byte) != 0 && ((1 << 5) & head_byte) != 0 && ((1 << 4) & head_byte) == 0)
            {
                return 3;
            }
            return -1;

        }

        // サイズを表すデータの桁数を取得する
        static int CountByte(byte[] bs, int address)
        {
            int num_byte = 1;
            for (int j = 0; ; j++)
            {
                // https://stackoverflow.com/questions/6758196/convert-int-to-a-bit-array-in-net
                BitArray b = new BitArray(new byte[] { bs[address + j] });
                int[] bits = b.Cast<bool>().Select(bit => bit ? 1 : 0).ToArray();
                Array.Reverse(bits);

                if (bits[0] == 1) num_byte++;
                else break;

            }
            return num_byte;
        }

        // サイズを表すデータを16進数から整数に変換する
        static int Byte2Int(byte[] bs, int address, int num_byte)
        {
            int[] result_bits = new int[7];
            for (int j = 0; j < num_byte; j++)
            {
                if (j == 0)
                {
                    // https://stackoverflow.com/questions/6758196/convert-int-to-a-bit-array-in-net
                    BitArray b = new BitArray(new byte[] { bs[address + j] });
                    int[] bits = b.Cast<bool>().Select(bit => bit ? 1 : 0).ToArray();
                    Array.Reverse(bits);

                    for (int k = 0; k < 7; k++)
                    {
                        result_bits[k] = bits[k + 1];
                    }
                }
                else
                {
                    // https://stackoverflow.com/questions/6758196/convert-int-to-a-bit-array-in-net
                    BitArray b = new BitArray(new byte[] { bs[address + j] });
                    int[] bits = b.Cast<bool>().Select(bit => bit ? 1 : 0).ToArray();
                    Array.Reverse(bits);

                    int[] temp_bits = new int[7];
                    for (int k = 0; k < 7; k++)
                    {
                        temp_bits[k] = bits[k + 1];
                    }

                    // https://dobon.net/vb/dotnet/programing/arraymerge.html
                    result_bits = temp_bits.Concat(result_bits).ToArray();

                }

            }

            return Convert.ToInt32(String.Join("", result_bits), 2);

        }
        static int Byte2Int(byte[] bs, int address)
        {

            int num_byte = CountByte(bs, address);

            int[] result_bits = new int[7];
            for (int j = 0; j < num_byte; j++)
            {
                if (j == 0)
                {
                    // https://stackoverflow.com/questions/6758196/convert-int-to-a-bit-array-in-net
                    BitArray b = new BitArray(new byte[] { bs[address + j] });
                    int[] bits = b.Cast<bool>().Select(bit => bit ? 1 : 0).ToArray();
                    Array.Reverse(bits);

                    for (int k = 0; k < 7; k++)
                    {
                        result_bits[k] = bits[k + 1];
                    }
                }
                else
                {
                    // https://stackoverflow.com/questions/6758196/convert-int-to-a-bit-array-in-net
                    BitArray b = new BitArray(new byte[] { bs[address + j] });
                    int[] bits = b.Cast<bool>().Select(bit => bit ? 1 : 0).ToArray();
                    Array.Reverse(bits);

                    int[] temp_bits = new int[7];
                    for (int k = 0; k < 7; k++)
                    {
                        temp_bits[k] = bits[k + 1];
                    }

                    // https://dobon.net/vb/dotnet/programing/arraymerge.html
                    result_bits = temp_bits.Concat(result_bits).ToArray();

                }

            }

            return Convert.ToInt32(String.Join("", result_bits), 2);

        }

        // 整数を16進数に変換するときの桁数を計算する
        static int CountInt(int num)
        {
            // https://stackoverflow.com/questions/6758196/convert-int-to-a-bit-array-in-net
            BitArray b = new BitArray(new int[] { num });
            int[] bits = b.Cast<bool>().Select(bit => bit ? 1 : 0).ToArray();
            Array.Reverse(bits);

            for (int i = 0; i < bits.Length; i++)
            {
                if (bits[i] == 1)
                {
                    double count = Math.Ceiling(1.0 * (32 - i) / 7);
                    return (int)count;
                }
            }

            return 0;

        }

        // 整数を16進数に変換する
        static byte[] Int2Byte(int num)
        {
            // 整数を2進数に変換、num -> bits
            // https://stackoverflow.com/questions/6758196/convert-int-to-a-bit-array-in-net
            BitArray b = new BitArray(new int[] { num });
            int[] bits = b.Cast<bool>().Select(bit => bit ? 1 : 0).ToArray();
            Array.Reverse(bits);

            // 2進数で表示
            //for (int i = 0; i < bits.Length; i++)
            //{
            //    Console.Write("{0} ", bits[i]);
            //}
            //Console.WriteLine("");

            int count = CountInt(num);

            byte[] bytes = new byte[count];
            byte[] temp_byte = new byte[1];

            for (int i = 0; i < count; i++)
            {
                // 右から7桁ずつBitArrayにコピー
                BitArray bit = new BitArray(8);
                for (int j = 0; j < 7; j++)
                {
                    if (31 - i * 7 - j < 0) break;
                    if (bits[31 - i * 7 - j] == 1) bit[j] = true;
                    else bit[j] = false;
                }

                // 一番大きな桁に、数値情報が次に続くかの情報を入れる
                if (i + 1 < count) bit[7] = true;
                else bit[7] = false;

                // BitArrayからバイト型配列に変換
                // https://stackoverflow.com/questions/560123/convert-from-bitarray-to-byte
                bit.CopyTo(temp_byte, 0);
                bytes[i] = temp_byte[0];

                //// 2進数で表示
                //for (int k = 0; k < 8; k++)
                //{
                //    if (bit[7 - k]) Console.Write("1 ");
                //    else Console.Write("0 ");
                //}
                //Console.WriteLine("");

            }

            return bytes;
        }

    }
}
