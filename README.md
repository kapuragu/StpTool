# StpTool
 A Fox Engine .stp Streamed Package file unpacker and repacker, based on BobDoleOwndU's AutoPftxsTool.

.stp files found in .sbp SoundBank Package files extracted by GzsTool contain streamed Wwise .wem RIFF audio files, and in TPP and onwards, also contain lip sync animation track files, here with the unofficial extension name .ls2.
.sab files found in .sbp files contain .ls and/or .st files in pairs. The former is the earlier version of TPP's .stp's .ls2 files, and .st files simply contain the subtitle id string. In GZ, the .sab subfiles are meant for both streamed .stp .wem voice clips, and the embedded ones in the .bnk Wwise SoundBank, but in TPP onwards, .st is never used, it's functionalty instead transferred to the embedded file marker in .wem files, and .ls files in .sab are only used for the embedded .wem files in the .bnk Wwise SoundBank, as the streamed .wems already have their lip sync files taken care of.
This tool also has basic .bnk Wwise SoundBank embedded Data Index section .wem dumping functionality.
