﻿using System;
using System.Text;
using System.Text.RegularExpressions;

public class JSDecoder {
	private const byte STATE_COPY_INPUT = 100;
	private const byte STATE_READLEN = 101;
	private const byte STATE_DECODE = 102;
	private const byte STATE_UNESCAPE = 103;

	private static byte[] _pickEncoding;
	private static byte[] _rawData;
	private static byte[] _digits = new byte[123];
	private static byte[][] _transformed = new byte[3][];

	static JSDecoder() {
		InitArrayData();
	}

	private static void InitArrayData() {
		_pickEncoding = new byte[] {
			1, 2, 0, 1, 2, 0, 2, 0, 0, 2, 0, 2, 1, 0, 2, 0, 
			1, 0, 2, 0, 1, 1, 2, 0, 0, 2, 1, 0, 2, 0, 0, 2, 
			1, 1, 0, 2, 0, 2, 0, 1, 0, 1, 1, 2, 0, 1, 0, 2, 
			1, 0, 2, 0, 1, 1, 2, 0, 0, 1, 1, 2, 0, 1, 0, 2
		};

		_rawData = new byte[] {
			0x64,0x37,0x69, 0x50,0x7E,0x2C, 0x22,0x5A,0x65, 0x4A,0x45,0x72,
			0x61,0x3A,0x5B, 0x5E,0x79,0x66, 0x5D,0x59,0x75, 0x5B,0x27,0x4C,
			0x42,0x76,0x45, 0x60,0x63,0x76, 0x23,0x62,0x2A, 0x65,0x4D,0x43,
			0x5F,0x51,0x33, 0x7E,0x53,0x42, 0x4F,0x52,0x20, 0x52,0x20,0x63,
			0x7A,0x26,0x4A, 0x21,0x54,0x5A, 0x46,0x71,0x38, 0x20,0x2B,0x79,
			0x26,0x66,0x32, 0x63,0x2A,0x57, 0x2A,0x58,0x6C, 0x76,0x7F,0x2B,
			0x47,0x7B,0x46, 0x25,0x30,0x52, 0x2C,0x31,0x4F, 0x29,0x6C,0x3D,
			0x69,0x49,0x70, 0x3F,0x3F,0x3F, 0x27,0x78,0x7B, 0x3F,0x3F,0x3F,
			0x67,0x5F,0x51, 0x3F,0x3F,0x3F, 0x62,0x29,0x7A, 0x41,0x24,0x7E,
			0x5A,0x2F,0x3B, 0x66,0x39,0x47, 0x32,0x33,0x41, 0x73,0x6F,0x77,
			0x4D,0x21,0x56, 0x43,0x75,0x5F, 0x71,0x28,0x26, 0x39,0x42,0x78,
			0x7C,0x46,0x6E, 0x53,0x4A,0x64, 0x48,0x5C,0x74, 0x31,0x48,0x67,
			0x72,0x36,0x7D, 0x6E,0x4B,0x68, 0x70,0x7D,0x35, 0x49,0x5D,0x22,
			0x3F,0x6A,0x55, 0x4B,0x50,0x3A, 0x6A,0x69,0x60, 0x2E,0x23,0x6A,
			0x7F,0x09,0x71, 0x28,0x70,0x6F, 0x35,0x65,0x49, 0x7D,0x74,0x5C,
			0x24,0x2C,0x5D, 0x2D,0x77,0x27, 0x54,0x44,0x59, 0x37,0x3F,0x25,
			0x7B,0x6D,0x7C, 0x3D,0x7C,0x23, 0x6C,0x43,0x6D, 0x34,0x38,0x28,
			0x6D,0x5E,0x31, 0x4E,0x5B,0x39, 0x2B,0x6E,0x7F, 0x30,0x57,0x36,
			0x6F,0x4C,0x54, 0x74,0x34,0x34, 0x6B,0x72,0x62, 0x4C,0x25,0x4E,
			0x33,0x56,0x30, 0x56,0x73,0x5E, 0x3A,0x68,0x73, 0x78,0x55,0x09,
			0x57,0x47,0x4B, 0x77,0x32,0x61, 0x3B,0x35,0x24, 0x44,0x2E,0x4D,
			0x2F,0x64,0x6B, 0x59,0x4F,0x44, 0x45,0x3B,0x21, 0x5C,0x2D,0x37,
			0x68,0x41,0x53, 0x36,0x61,0x58, 0x58,0x7A,0x48, 0x79,0x22,0x2E,
			0x09,0x60,0x50, 0x75,0x6B,0x2D, 0x38,0x4E,0x29, 0x55,0x3D,0x3F
		};

		for (byte i = 0; i < 3; i++) _transformed[i] = new byte[288];
		for (byte i = 31; i < 127; i++) for (byte j = 0; j < 3; j++) _transformed[j][_rawData[(i - 31) * 3 + j]] = i == 31 ? (byte)9 : i;

		for (byte i = 0; i < 26; i++) {
			_digits[65 + i] = i;
			_digits[97 + i] = (byte)(i + 26);
		}

		for (byte i = 0; i < 10; i++)
			_digits[48 + i] = (byte)(i + 52);

		_digits[43] = 62;
		_digits[47] = 63;
	}

	private static string UnEscape(string s) {
		string escapes = "#&!*$";
		string escaped = "\r\n<>@";

		if ((int)s.ToCharArray()[0] > 126) return s;
		if (escapes.IndexOf(s) != -1) return escaped.Substring(escapes.IndexOf(s), 1);
		return "?";
	}

	private static int DecodeBase64(string s) {
		int val = 0;
		byte[] bs = Encoding.UTF8.GetBytes(s);

		val += ((int)_digits[bs[0]] << 2);
		val += (_digits[bs[1]] >> 4);
		val += (_digits[bs[1]] & 0xf) << 12;
		val += ((_digits[bs[2]] >> 2) << 8);
		val += ((_digits[bs[2]] & 0x3) << 22);
		val += (_digits[bs[3]] << 16);
		return val;
	}

	public static string Decode(string encodingString) {
		string marker = "#@~^";
		int stringIndex = 0;
		int scriptIndex = -1;
		int unEncodingIndex = 0;
		string strChar = "";
		string getCodeString = "";
		int unEncodinglength = 0;
		int state = STATE_COPY_INPUT;
		string unEncodingString = "";

		try {
			while (state != 0) {
				switch (state) {
					case STATE_COPY_INPUT:

						scriptIndex = encodingString.IndexOf(marker, stringIndex);
						if (scriptIndex != -1) {
							unEncodingString += encodingString.Substring(stringIndex, scriptIndex);
							scriptIndex += marker.Length;
							state = STATE_READLEN;
						} else {
							stringIndex = stringIndex == 0 ? 0 : stringIndex;
							unEncodingString += encodingString.Substring(stringIndex);
							state = 0;
						}
						break;
					case STATE_READLEN:

						getCodeString = encodingString.Substring(scriptIndex, 6);
						unEncodinglength = DecodeBase64(getCodeString);
						scriptIndex += 8;
						state = STATE_DECODE;
						break;
					case STATE_DECODE:

						if (unEncodinglength == 0) {
							stringIndex = scriptIndex + "DQgAAA==^#~@".Length;
							unEncodingIndex = 0;
							state = STATE_COPY_INPUT;
						} else {
							strChar = encodingString.Substring(scriptIndex, 1);
							if (strChar == "@") state = STATE_UNESCAPE;
							else {
								int b = (int)strChar.ToCharArray()[0];
								if (b < 0xFF) {
									unEncodingString += (char)_transformed[_pickEncoding[unEncodingIndex % 64]][b];
									unEncodingIndex++;
								} else {
									unEncodingString += strChar;
								}
								scriptIndex++;
								unEncodinglength--;
							}
						}
						break;
					case STATE_UNESCAPE:

						unEncodingString += UnEscape(encodingString.Substring(++scriptIndex, 1));
						scriptIndex++;
						unEncodinglength -= 2;
						unEncodingIndex++;
						state = STATE_DECODE;
						break;
				}
			}
		} catch { }
		string Pattern;
		Pattern = "(JScript|VBscript).encode";
		unEncodingString = Regex.Replace(unEncodingString, Pattern, "", RegexOptions.IgnoreCase);
		return unEncodingString;
	}
}