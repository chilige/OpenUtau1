# 改动内容
## 1. 音素器
加入了几个DiffSinger Rhythmizer Phonemizer，可以将其他语种或其他派系的音素映射到牛夫人的Rhythmizer上（可自定义），并使用其预测音素时长。
歌手目录下需要准备`rhy_map.txt`音素映射文件、`ds_XXX.txt`语种字典：`ds_CNM|ds_CNY|ds_JPN|ds_ENG|ds_KOR`
### rhy_map.txt
```
k1[需映射音素],k[对应rhy里面的音素]
```

### ds_XXX.txt
```
ka,k a
kaa,k a a
br,AP
```
注：DiffSinger Mandarin Rhythmizer Phonemizer 和 DiffSinger Mandarin DIY Rhythmizer Phonemizer的区别是：前者只能用牛夫人里面的音素表和rhy，后者可以使用自定义的中文字典`ds_CNM.txt`。

### diy_rhy.txt （可选）
自定义语种对应的rhythmizer
```
JPN,【rhy目录名，放在Dependencies目录下的】
CHY,cny_rhy_name
CNM,rhy_name
ENG,rhy_name
KOR,rhy_name
```
所以目前的加载优先级是 1. 歌手目录自带的 2. diy_rhy.txt里面定义的 3. 牛夫人的
## 2. 拼音转换
### 转换汉语拼音
DiffSinger Mandarin Rhythmizer Phonemizer 和 DiffSinger Mandarin DIY Rhythmizer Phonemizer，可直接输入汉字，会自动转拼音。

### 转换粤语拼音
DiffSinger Cantonese Rhythmizer Phonemizer，可直接输入汉字（简体），会自动转粤语拼音。
此功能为简单单子转换，待小狼的中文转换合入再接入其使用

### 转换日语罗马拼音
DiffSinger Japanese Rhythmizer Phonemizer，可直接输入假名，会自动转换罗马音，字典里面无需再申明假名转换。

## 3. 歌词编辑功能
自动添加AP，SP音素，汉字转换粤语拼音，字母大小写转换

## 后续
1. 增加三段式中文专用的音素器：字典`ds_CNM3.txt`
2. 在两个有自定义词典的中文音素器中增加歌词后缀换词典解析功能。&2用二段中文解析，&3用三段中文解析，&j用日语解析。
