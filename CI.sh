#!/bin/bash

time=$(date "+%Y%m%d%H%M%S")

echo "Start to build the project from CI tool."
echo "Input the modified comment:"
read comment

# 提交 Commit
git add .
git commit -m "$comment"
git push
git push gitlab

# 创建缓存工程
releaseName=ShanghaiWindyPublicResources-Release
output=$time-ShanghaiWindyPublicResources.zip

mkdir $releaseName

cp -r  com.jbooth.microverse $releaseName/com.jbooth.microverse
cp -r  com.jbooth.microverse.ambiance $releaseName/com.jbooth.microverse.ambiance
cp -r  com.jbooth.microverse.demo $releaseName/com.jbooth.microverse.demo
cp -r  com.jbooth.microverse.vegetation $releaseName/com.jbooth.microverse.vegetation
cp -r  com.unity.splines $releaseName/com.unity.splines
cp -r  Shares $releaseName/Shares

./zip.exe -r $output $releaseName

rm -r -f $releaseName
mv $output archive/$output