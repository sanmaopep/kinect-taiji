﻿using KinectCore.core;
using KinectCore.model;
using KinectCore.util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using TaichiUI_teacher.ViewModels;

namespace TaichiUI_teacher
{
    /// <summary>
    /// Record.xaml 的交互逻辑
    /// </summary>
    public partial class Record : UserControl
    {

        private KinectControl kcRecorder = new KinectControl();
        private FeaturePainter featurePainter;
        private MotionQuality motionQuality;
        private const int DELAY_SECONDS = 20;

        private bool recordFlag = true; // 控制只录制一次
        private bool saveFileFlag = true;


        public Record() => InitializeComponent();

        // 收到一个帧
        private void getRecordFeature(Feature feature)
        {
            imgRgb.Source = feature.rgbImage.imageSource;
            featurePainter.paint(feature);

            if (feature.ok && recordFlag)
            {
                startRecord();
            }

            if (motionQuality.getLatestQuality() < 10 && saveFileFlag)
            {
                stopRecord();
            }
        }

        private void UserControlLoaded(object sender, RoutedEventArgs e)
        {
            // 初始化设备
            kcRecorder.InitializeFaculty();
            kcRecorder.featureReady += getRecordFeature;

            // 渲染骨骼
            featurePainter = new FeaturePainter(kcRecorder);
            imgSkeleton.Source = featurePainter.getImageSource();
            // 运动量计算
            motionQuality = new MotionQuality(kcRecorder.featureBuffer);

            tbNotice.Text = "正在检测人物";
        }

        // 开始录制
        private async void startRecord()
        {
            recordFlag = false;
            for (int i = 0; i < DELAY_SECONDS; i++)
            {
                await Task.Run(() => Thread.Sleep(1000));
                tbNotice.Text = "检测到人物" + (DELAY_SECONDS - i) + "秒后开始录制";
            }

            tbNotice.Text = "开始进行动作，当静止时会自动停止录制！";
            kcRecorder.record = true;
            kcRecorder.recordRgb = true;
        }

        // 停止录制
        private async void stopRecord()
        {
            saveFileFlag = false;
            tbNotice.Text = "动作静止，停止录制，正在保存文件";
            kcRecorder.record = false;
            kcRecorder.recordRgb = false;
            // 保存到文件
            string filename = CreateShortToken() + @".dat";
            await Task.Run(() => kcRecorder.saveToFile(MainWindowModel.MOTION_LIB_PATH + "/" + filename));
            // 更新motion.json
            SingleMotionModel[] models = ((MainWindowModel)DataContext).homeModel.singleMotionModels;
            SingleMotionModel curr = new SingleMotionModel();
            curr.title = "未命名";
            curr.description = "未命名";
            curr.data = filename;
            List<SingleMotionModel> list = models.ToList();
            list.Add(curr);

            MotionLibsUtil.saveToFile(list.ToArray(), MainWindowModel.MOTION_LIB_PATH);

            tbNotice.Text = "成功保存文件";
        }



        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            kcRecorder.stopFaculty();
        }


        public string CreateShortToken()
        {
            var rnd = new Random();
            var tokenData = new byte[8];
            rnd.NextBytes(tokenData);
            var token = Convert.ToBase64String(tokenData).Replace("=", "").Replace("+", "").Replace("/", "");
            return token;
        }
    }
}
