﻿using System;
using System.Collections.Generic;
using System.Text;

namespace LTE.InternalInterference
{
    class KREGain:AbstrGain
    {
         public override double[] GetHAGain()
        {
            HAGain[0] = 0;
            HAGain[1] = 0;
            HAGain[2] = 0;
            HAGain[3] = 0;
            HAGain[4] = 0.1;
            HAGain[5] = 0.1;
            HAGain[6] = 0.1;
            HAGain[7] = 0.2;
            HAGain[8] = 0.2;
            HAGain[9] = 0.2;
            HAGain[10] = 0.3;
            HAGain[11] = 0.4;
            HAGain[12] = 0.4;
            HAGain[13] = 0.5;
            HAGain[14] = 0.6;
            HAGain[15] = 0.7;
            HAGain[16] = 0.7;
            HAGain[17] = 0.8;
            HAGain[18] = 0.9;
            HAGain[19] = 1.1;
            HAGain[20] = 1.2;
            HAGain[21] = 1.3;
            HAGain[22] = 1.4;
            HAGain[23] = 1.5;
            HAGain[24] = 1.7;
            HAGain[25] = 1.8;
            HAGain[26] = 1.9;
            HAGain[27] = 2.1;
            HAGain[28] = 2.2;
            HAGain[29] = 2.4;
            HAGain[30] = 2.5;
            HAGain[31] = 2.7;
            HAGain[32] = 2.9;
            HAGain[33] = 3;
            HAGain[34] = 3.2;
            HAGain[35] = 3.4;
            HAGain[36] = 3.6;
            HAGain[37] = 3.8;
            HAGain[38] = 4;
            HAGain[39] = 4.2;
            HAGain[40] = 4.4;
            HAGain[41] = 4.6;
            HAGain[42] = 4.8;
            HAGain[43] = 5.0;
            HAGain[44] = 5.2;
            HAGain[45] = 5.5;
            HAGain[46] = 5.7;
            HAGain[47] = 5.9;
            HAGain[48] = 6.2;
            HAGain[49] = 6.4;
            HAGain[50] = 6.7;
            HAGain[51] = 6.9;
            HAGain[52] = 7.2;
            HAGain[53] = 7.4;
            HAGain[54] = 7.7;
            HAGain[55] = 8.0;
            HAGain[56] = 8.2;
            HAGain[57] = 8.5;
            HAGain[58] = 8.8;
            HAGain[59] = 9.0;
            HAGain[60] = 9.3;
            HAGain[61] = 9.6;
            HAGain[62] = 9.8;
            HAGain[63] = 10.1;
            HAGain[64] = 10.4;
            HAGain[65] = 10.7;
            HAGain[66] = 11.0;
            HAGain[67] = 11.2;
            HAGain[68] = 11.5;
            HAGain[69] = 11.8;
            HAGain[70] = 12.1;
            HAGain[71] = 12.4;
            HAGain[72] = 12.7;
            HAGain[73] = 13.0;
            HAGain[74] = 13.3;
            HAGain[75] = 13.6;
            HAGain[76] = 13.9;
            HAGain[77] = 14.2;
            HAGain[78] = 14.5;
            HAGain[79] = 14.7;
            HAGain[80] = 15.0;
            HAGain[81] = 15.3;
            HAGain[82] = 15.6;
            HAGain[83] = 15.9;
            HAGain[84] = 16.2;
            HAGain[85] = 16.5;
            HAGain[86] = 16.8;
            HAGain[87] = 17.1;
            HAGain[88] = 17.4;
            HAGain[89] = 17.7;
            HAGain[90] = 18.0;
            HAGain[91] = 18.2;
            HAGain[92] = 18.5;
            HAGain[93] = 18.9;
            HAGain[94] = 19.1;
            HAGain[95] = 19.4;
            HAGain[96] = 19.7;
            HAGain[97] = 20.0;
            HAGain[98] = 20.3;
            HAGain[99] = 20.6;
            HAGain[100] = 20.9;
            HAGain[101] = 21.1;
            HAGain[102] = 21.4;
            HAGain[103] = 21.7;
            HAGain[104] = 21.9;
            HAGain[105] = 22.2;
            HAGain[106] = 22.5;
            HAGain[107] = 22.7;
            HAGain[108] = 23.0;
            HAGain[109] = 23.2;
            HAGain[110] = 23.5;
            HAGain[111] = 23.7;
            HAGain[112] = 23.9;
            HAGain[113] = 24.2;
            HAGain[114] = 24.4;
            HAGain[115] = 24.6;
            HAGain[116] = 24.8;
            HAGain[117] = 25.0;
            HAGain[118] = 25.2;
            HAGain[119] = 25.4;
            HAGain[120] = 25.6;
            HAGain[121] = 25.8;
            HAGain[122] = 26.0;
            HAGain[123] = 26.2;
            HAGain[124] = 26.4;
            HAGain[125] = 26.6;
            HAGain[126] = 26.8;
            HAGain[127] = 27.0;
            HAGain[128] = 27.1;
            HAGain[129] = 27.3;
            HAGain[130] = 27.4;
            HAGain[131] = 27.6;
            HAGain[132] = 27.8;
            HAGain[133] = 27.9;
            HAGain[134] = 28.1;
            HAGain[135] = 28.2;
            HAGain[136] = 28.5;
            HAGain[137] = 28.6;
            HAGain[138] = 28.7;
            HAGain[139] = 28.9;
            HAGain[140] = 29.1;
            HAGain[141] = 29.2;
            HAGain[142] = 29.4;
            HAGain[143] = 29.6;
            HAGain[144] = 29.8;
            HAGain[145] = 30.0;
            HAGain[146] = 30.2;
            HAGain[147] = 30.5;
            HAGain[148] = 30.6;
            HAGain[149] = 30.9;
            HAGain[150] = 31.0;
            HAGain[151] = 31.2;
            HAGain[152] = 31.5;
            HAGain[153] = 31.7;
            HAGain[154] = 31.9;
            HAGain[155] = 32.2;
            HAGain[156] = 32.5;
            HAGain[157] = 32.7;
            HAGain[158] = 33.0;
            HAGain[159] = 33.3;
            HAGain[160] = 33.6;
            HAGain[161] = 33.9;
            HAGain[162] = 34.2;
            HAGain[163] = 34.4;
            HAGain[164] = 34.7;
            HAGain[165] = 35.1;
            HAGain[166] = 35.3;
            HAGain[167] = 35.7;
            HAGain[168] = 36.0;
            HAGain[169] = 36.2;
            HAGain[170] = 36.5;
            HAGain[171] = 36.7;
            HAGain[172] = 37.0;
            HAGain[173] = 37.3;
            HAGain[174] = 37.6;
            HAGain[175] = 37.8;
            HAGain[176] = 38.0;
            HAGain[177] = 38.2;
            HAGain[178] = 38.3;
            HAGain[179] = 38.5;
            HAGain[180] = 38.6;
            HAGain[181] = 38.7;
            HAGain[182] = 38.8;
            HAGain[183] = 38.9;
            HAGain[184] = 38.9;
            HAGain[185] = 39.1;
            HAGain[186] = 39.2;
            HAGain[187] = 39.2;
            HAGain[188] = 39.3;
            HAGain[189] = 39.5;
            HAGain[190] = 39.7;
            HAGain[191] = 39.8;
            HAGain[192] = 40.0;
            HAGain[193] = 40.4;
            HAGain[194] = 40.5;
            HAGain[195] = 40.7;
            HAGain[196] = 41.0;
            HAGain[197] = 41.3;
            HAGain[198] = 41.6;
            HAGain[199] = 41.9;
            HAGain[200] = 42.2;
            HAGain[201] = 42.4;
            HAGain[202] = 42.7;
            HAGain[203] = 42.8;
            HAGain[204] = 42.9;
            HAGain[205] = 43.1;
            HAGain[206] = 42.9;
            HAGain[207] = 42.6;
            HAGain[208] = 42.1;
            HAGain[209] = 41.6;
            HAGain[210] = 41.1;
            HAGain[211] = 40.3;
            HAGain[212] = 39.6;
            HAGain[213] = 38.9;
            HAGain[214] = 38.2;
            HAGain[215] = 37.5;
            HAGain[216] = 36.9;
            HAGain[217] = 36.3;
            HAGain[218] = 35.7;
            HAGain[219] = 35.1;
            HAGain[220] = 34.5;
            HAGain[221] = 33.9;
            HAGain[222] = 33.4;
            HAGain[223] = 32.9;
            HAGain[224] = 32.4;
            HAGain[225] = 32.0;
            HAGain[226] = 31.5;
            HAGain[227] = 31.1;
            HAGain[228] = 30.7;
            HAGain[229] = 30.3;
            HAGain[230] = 30.0;
            HAGain[231] = 29.6;
            HAGain[232] = 29.3;
            HAGain[233] = 29.0;
            HAGain[234] = 28.7;
            HAGain[235] = 28.4;
            HAGain[236] = 28.1;
            HAGain[237] = 27.8;
            HAGain[238] = 27.5;
            HAGain[239] = 27.2;
            HAGain[240] = 26.9;
            HAGain[241] = 26.6;
            HAGain[242] = 26.4;
            HAGain[243] = 26.1;
            HAGain[244] = 25.8;
            HAGain[245] = 25.6;
            HAGain[246] = 25.3;
            HAGain[247] = 25.0;
            HAGain[248] = 24.7;
            HAGain[249] = 24.4;
            HAGain[250] = 24.1;
            HAGain[251] = 23.8;
            HAGain[252] = 23.6;
            HAGain[253] = 23.3;
            HAGain[254] = 23.0;
            HAGain[255] = 22.7;
            HAGain[256] = 22.4;
            HAGain[257] = 22.1;
            HAGain[258] = 21.9;
            HAGain[259] = 21.6;
            HAGain[260] = 21.3;
            HAGain[261] = 21.0;
            HAGain[262] = 20.7;
            HAGain[263] = 20.5;
            HAGain[264] = 20.2;
            HAGain[265] = 19.9;
            HAGain[266] = 19.6;
            HAGain[267] = 19.3;
            HAGain[268] = 19.0;
            HAGain[269] = 18.7;
            HAGain[270] = 18.4;
            HAGain[271] = 18.1;
            HAGain[272] = 17.8;
            HAGain[273] = 17.5;
            HAGain[274] = 17.2;
            HAGain[275] = 16.9;
            HAGain[276] = 16.6;
            HAGain[277] = 16.3;
            HAGain[278] = 16.0;
            HAGain[279] = 15.7;
            HAGain[280] = 15.4;
            HAGain[281] = 15.1;
            HAGain[282] = 14.8;
            HAGain[283] = 14.4;
            HAGain[284] = 14.2;
            HAGain[285] = 13.8;
            HAGain[286] = 13.5;
            HAGain[287] = 13.2;
            HAGain[288] = 12.9;
            HAGain[289] = 12.6;
            HAGain[290] = 12.3;
            HAGain[291] = 12.0;
            HAGain[292] = 11.7;
            HAGain[293] = 11.4;
            HAGain[294] = 11.1;
            HAGain[295] = 10.8;
            HAGain[296] = 10.5;
            HAGain[297] = 10.2;
            HAGain[298] = 9.9;
            HAGain[299] = 9.6;
            HAGain[300] = 9.3;
            HAGain[301] = 9.0;
            HAGain[302] = 8.7;
            HAGain[303] = 8.5;
            HAGain[304] = 8.2;
            HAGain[305] = 7.9;
            HAGain[306] = 7.7;
            HAGain[307] = 7.4;
            HAGain[308] = 7.2;
            HAGain[309] = 6.9;
            HAGain[310] = 6.7;
            HAGain[311] = 6.4;
            HAGain[312] = 6.2;
            HAGain[313] = 5.9;
            HAGain[314] = 5.7;
            HAGain[315] = 5.5;
            HAGain[316] = 5.2;
            HAGain[317] = 5.0;
            HAGain[318] = 4.8;
            HAGain[319] = 4.5;
            HAGain[320] = 4.3;
            HAGain[321] = 4.1;
            HAGain[322] = 3.9;
            HAGain[323] = 3.7;
            HAGain[324] = 3.5;
            HAGain[325] = 3.3;
            HAGain[326] = 3.1;
            HAGain[327] = 3.0;
            HAGain[328] = 2.8;
            HAGain[329] = 2.6;
            HAGain[330] = 2.5;
            HAGain[331] = 2.3;
            HAGain[332] = 2.1;
            HAGain[333] = 2.0;
            HAGain[334] = 1.8;
            HAGain[335] = 1.7;
            HAGain[336] = 1.5;
            HAGain[337] = 1.4;
            HAGain[338] = 1.3;
            HAGain[339] = 1.2;
            HAGain[340] = 1.1;
            HAGain[341] = 0.9;
            HAGain[342] = 0.8;
            HAGain[343] = 0.7 ;
            HAGain[344] = 0.7;
            HAGain[345] = 0.6;
            HAGain[346] = 0.5;
            HAGain[347] = 0.4;
            HAGain[348] = 0.3;
            HAGain[349] = 0.3;
            HAGain[350] = 0.2;
            HAGain[351] = 0.2;
            HAGain[352] = 0.1;
            HAGain[353] = 0.1;
            HAGain[354] = 0.1;
            HAGain[355] = 0.1;
            HAGain[356] = 0;
            HAGain[357] = 0;
            HAGain[358] = 0.0;
            HAGain[359] = 0.0;

            return HAGain;
        }
        public override double[] GetVAGain()
        {
            VAGain[0] = 0;
            VAGain[1] = 0.1;
            VAGain[2] = 0.3;
            VAGain[3] = 0.6;
            VAGain[4] = 1;
            VAGain[5] = 1.6;
            VAGain[6] = 2.3;
            VAGain[7] = 3.1;
            VAGain[8] = 4.2;
            VAGain[9] = 5.4;
            VAGain[10] = 6.9;
            VAGain[11] = 8.7;
            VAGain[12] = 10.9;
            VAGain[13] = 13.9;
            VAGain[14] = 17.9;
            VAGain[15] = 25.2;
            VAGain[16] = 36.0;
            VAGain[17] = 23.4;
            VAGain[18] = 18.7;
            VAGain[19] = 16.1;
            VAGain[20] = 14.6;
            VAGain[21] = 13.7;
            VAGain[22] = 13.1;
            VAGain[23] = 12.9;
            VAGain[24] = 13.0;
            VAGain[25] = 13.3;
            VAGain[26] = 13.9;
            VAGain[27] = 14.7;
            VAGain[28] = 15.9;
            VAGain[29] = 17.3;
            VAGain[30] = 19.1;
            VAGain[31] = 21.5;
            VAGain[32] = 24.6;
            VAGain[33] = 28.4;
            VAGain[34] = 30.8;
            VAGain[35] = 28.4;
            VAGain[36] = 25.4;
            VAGain[37] = 23.1;
            VAGain[38] = 21.5;
            VAGain[39] = 20.4;
            VAGain[40] = 19.6;
            VAGain[41] = 19.1;
            VAGain[42] = 18.9;
            VAGain[43] = 18.9;
            VAGain[44] = 19.1;
            VAGain[45] = 19.4;
            VAGain[46] = 19.9;
            VAGain[47] = 20.6;
            VAGain[48] = 21.5;
            VAGain[49] = 22.6;
            VAGain[50] = 24.1;
            VAGain[51] = 25.8;
            VAGain[52] = 28.0;
            VAGain[53] = 31.1;
            VAGain[54] = 35.6;
            VAGain[55] = 43.5;
            VAGain[56] = 42.5;
            VAGain[57] = 35.7;
            VAGain[58] = 31.8;
            VAGain[59] = 29.3;
            VAGain[60] = 27.6;
            VAGain[61] = 26.2;
            VAGain[62] = 25.2;
            VAGain[63] = 24.4;
            VAGain[64] = 23.8;
            VAGain[65] = 23.3;
            VAGain[66] = 22.9;
            VAGain[67] = 22.6;
            VAGain[68] = 22.4;
            VAGain[69] = 22.3;
            VAGain[70] = 22.2;
            VAGain[71] = 22.1;
            VAGain[72] = 22.1;
            VAGain[73] = 22.1;
            VAGain[74] = 22.2;
            VAGain[75] = 22.3;
            VAGain[76] = 22.4;
            VAGain[77] = 22.5;
            VAGain[78] = 22.6;
            VAGain[79] = 22.8;
            VAGain[80] = 23.0;
            VAGain[81] = 23.1;
            VAGain[82] = 23.3;
            VAGain[83] = 23.5;
            VAGain[84] = 23.7;
            VAGain[85] = 23.9;
            VAGain[86] = 24.1;
            VAGain[87] = 24.4;
            VAGain[88] = 24.6;
            VAGain[89] = 24.9;
            VAGain[90] = 25.1;
            VAGain[91] = 25.3;
            VAGain[92] = 25.6;
            VAGain[93] = 25.9;
            VAGain[94] = 26.1;
            VAGain[95] = 26.4;
            VAGain[96] = 26.7;
            VAGain[97] = 26.9;
            VAGain[98] = 27.2;
            VAGain[99] = 27.6;
            VAGain[100] = 27.9;
            VAGain[101] = 28.3;
            VAGain[102] = 28.6;
            VAGain[103] = 29.1;
            VAGain[104] = 29.5;
            VAGain[105] = 30.0;
            VAGain[106] = 30.5;
            VAGain[107] = 31.1;
            VAGain[108] = 31.8;
            VAGain[109] = 32.5;
            VAGain[110] = 33.4;
            VAGain[111] = 34.3;
            VAGain[112] = 35.4;
            VAGain[113] = 36.5;
            VAGain[114] = 37.8;
            VAGain[115] = 39.1;
            VAGain[116] = 40.6;
            VAGain[117] = 42.2;
            VAGain[118] = 43.8;
            VAGain[119] = 44.7;
            VAGain[120] = 44.8;
            VAGain[121] = 44.1;
            VAGain[122] = 42.8;
            VAGain[123] = 41.8;
            VAGain[124] = 40.9;
            VAGain[125] = 40.3;
            VAGain[126] = 39.9;
            VAGain[127] = 39.7;
            VAGain[128] = 39.9;
            VAGain[129] = 40.4;
            VAGain[130] = 41.0;
            VAGain[131] = 42.3;
            VAGain[132] = 43.8;
            VAGain[133] = 47.0;
            VAGain[134] = 51.2;
            VAGain[135] = 55.9;
            VAGain[136] = 51.3;
            VAGain[137] = 45.9;
            VAGain[138] = 42.7;
            VAGain[139] = 40.0;
            VAGain[140] = 38.2;
            VAGain[141] = 36.6;
            VAGain[142] = 35.5;
            VAGain[143] = 34.7;
            VAGain[144] = 34.1;
            VAGain[145] = 33.7;
            VAGain[146] = 33.4;
            VAGain[147] = 33.4;
            VAGain[148] = 33.5;
            VAGain[149] = 33.9;
            VAGain[150] = 34.4;
            VAGain[151] = 35.3;
            VAGain[152] = 36.2;
            VAGain[153] = 37.6;
            VAGain[154] = 39.2;
            VAGain[155] = 41.3;
            VAGain[156] = 43.9;
            VAGain[157] = 46.7;
            VAGain[158] = 47.5;
            VAGain[159] = 45.1;
            VAGain[160] = 42.6;
            VAGain[161] = 41.1;
            VAGain[162] = 39.7;
            VAGain[163] = 38.8;
            VAGain[164] = 38.3;
            VAGain[165] = 38.0;
            VAGain[166] = 38.0;
            VAGain[167] = 38.1;
            VAGain[168] = 38.5;
            VAGain[169] = 38.9;
            VAGain[170] = 39.4;
            VAGain[171] = 40.1;
            VAGain[172] = 40.3;
            VAGain[173] = 40.8;
            VAGain[174] = 41.1;
            VAGain[175] = 40.8;
            VAGain[176] = 40.8;
            VAGain[177] = 40.6;
            VAGain[178] = 40.4;
            VAGain[179] = 40.7;
            VAGain[180] = 41.3;
            VAGain[181] = 41.7;
            VAGain[182] = 42.9;
            VAGain[183] = 44.7;
            VAGain[184] = 47.2;
            VAGain[185] = 51.3;
            VAGain[186] = 63.7;
            VAGain[187] = 58.4;
            VAGain[188] = 48.5;
            VAGain[189] = 45.0;
            VAGain[190] = 42.6;
            VAGain[191] = 40.2;
            VAGain[192] = 38.9;
            VAGain[193] = 37.9;
            VAGain[194] = 37.3;
            VAGain[195] = 36.9;
            VAGain[196] = 36.8;
            VAGain[197] = 37.0;
            VAGain[198] = 37.3;
            VAGain[199] = 38.2;
            VAGain[200] = 39.1;
            VAGain[201] = 40.7;
            VAGain[202] = 42.9;
            VAGain[203] = 46.0;
            VAGain[204] = 49.6;
            VAGain[205] = 50.4;
            VAGain[206] = 46.5;
            VAGain[207] = 43.1;
            VAGain[208] = 40.9;
            VAGain[209] = 39.0;
            VAGain[210] = 37.7;
            VAGain[211] = 36.7;
            VAGain[212] = 36.0;
            VAGain[213] = 35.5;
            VAGain[214] = 35.3;
            VAGain[215] = 35.3;
            VAGain[216] = 35.5;
            VAGain[217] = 35.9;
            VAGain[218] = 36.6;
            VAGain[219] = 37.6;
            VAGain[220] = 38.8;
            VAGain[221] = 40.5;
            VAGain[222] = 42.9;
            VAGain[223] = 46.1;
            VAGain[224] = 52.1;
            VAGain[225] = 61.3;
            VAGain[226] = 51.2;
            VAGain[227] = 46.4;
            VAGain[228] = 43.1;
            VAGain[229] = 41.0;
            VAGain[230] = 39.4;
            VAGain[231] = 38.3;
            VAGain[232] = 37.5;
            VAGain[233] = 36.9;
            VAGain[234] = 36.4;
            VAGain[235] = 36.1;
            VAGain[236] = 35.9;
            VAGain[237] = 35.8;
            VAGain[238] = 35.8;
            VAGain[239] = 35.8;
            VAGain[240] = 35.9;
            VAGain[241] = 36.0;
            VAGain[242] = 36.0;
            VAGain[243] = 35.9;
            VAGain[244] = 35.8;
            VAGain[245] = 35.6;
            VAGain[246] = 35.4;
            VAGain[247] = 35.0;
            VAGain[248] = 34.5;
            VAGain[249] = 34.0;
            VAGain[250] = 33.6;
            VAGain[251] = 33.0;
            VAGain[252] = 32.5;
            VAGain[253] = 32.0;
            VAGain[254] = 31.6;
            VAGain[255] = 31.1;
            VAGain[256] = 30.6;
            VAGain[257] = 30.2;
            VAGain[258] = 29.7;
            VAGain[259] = 29.1;
            VAGain[260] = 28.7;
            VAGain[261] = 28.3;
            VAGain[262] = 27.9;
            VAGain[263] = 27.5;
            VAGain[264] = 27.1;
            VAGain[265] = 26.6;
            VAGain[266] = 26.2;
            VAGain[267] = 25.9;
            VAGain[268] = 25.5;
            VAGain[269] = 25.2;
            VAGain[270] = 24.8;
            VAGain[271] = 24.5;
            VAGain[272] = 24.1;
            VAGain[273] = 23.7;
            VAGain[274] = 23.3;
            VAGain[275] = 23.0;
            VAGain[276] = 22.7;
            VAGain[277] = 22.4;
            VAGain[278] = 22.1;
            VAGain[279] = 21.9;
            VAGain[280] = 21.7;
            VAGain[281] = 21.4;
            VAGain[282] = 21.1;
            VAGain[283] = 21.0;
            VAGain[284] = 20.9;
            VAGain[285] = 20.8;
            VAGain[286] = 20.7;
            VAGain[287] = 20.7;
            VAGain[288] = 20.6;
            VAGain[289] = 20.7;
            VAGain[290] = 20.8;
            VAGain[291] = 20.9;
            VAGain[292] = 21.1;
            VAGain[293] = 21.4;
            VAGain[294] = 21.8;
            VAGain[295] = 22.2;
            VAGain[296] = 22.8;
            VAGain[297] = 23.5;
            VAGain[298] = 24.4;
            VAGain[299] = 25.5;
            VAGain[300] = 26.8;
            VAGain[301] = 28.4;
            VAGain[302] = 30.4;
            VAGain[303] = 32.3;
            VAGain[304] =33.2;
            VAGain[305] = 31.9;
            VAGain[306] = 29.5;
            VAGain[307] = 27.2;
            VAGain[308] = 25.2;
            VAGain[309] = 23.5;
            VAGain[310] =22.1;
            VAGain[311] = 21.0;
            VAGain[312] = 20.0;
            VAGain[313] = 19.3;
            VAGain[314] = 18.7;
            VAGain[315] = 18.3;
            VAGain[316] = 18.0;
            VAGain[317] = 17.9;
            VAGain[318] = 18.0;
            VAGain[319] = 18.2;
            VAGain[320] = 18.7;
            VAGain[321] = 19.4;
            VAGain[322] = 20.5;
            VAGain[323] = 22.1;
            VAGain[324] = 24.1;
            VAGain[325] = 27.1;
            VAGain[326] = 30.0;
            VAGain[327] = 28.9;
            VAGain[328] = 25.2;
            VAGain[329] = 21.9;
            VAGain[330] = 19.4;
            VAGain[331] = 17.6;
            VAGain[332] = 16.1;
            VAGain[333] = 15.0;
            VAGain[334] = 14.1;
            VAGain[335] = 13.6;
            VAGain[336] = 13.2;
            VAGain[337] = 13.2;
            VAGain[338] = 13.5;
            VAGain[339] = 14.1;
            VAGain[340] = 15.2;
            VAGain[341] = 16.9;
            VAGain[342] = 19.8;
            VAGain[343] = 25.1;
            VAGain[344] = 32.1;
            VAGain[345] = 22.1;
            VAGain[346] = 16.6;
            VAGain[347] = 12.9;
            VAGain[348] = 10.3;
            VAGain[349] = 8.2;
            VAGain[350] = 6.4;
            VAGain[351] = 5.0;
            VAGain[352] = 3.8;
            VAGain[353] = 2.9;
            VAGain[354] = 2.0;
            VAGain[355] = 1.4;
            VAGain[356] = 0.9;
            VAGain[357] = 0.5;
            VAGain[358] = 0.2;
            VAGain[359] = 0.1;

            return VAGain;
        }
    }
}
