using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace WordCloudControl
{
    public class WordCloud : Control, INotifyPropertyChanged
    {
        // Using a DependencyProperty as the backing store for Entries.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty EntriesProperty =
            DependencyProperty.Register("Entries", typeof(ObservableCollection<WordCloudEntry>), typeof(WordCloud), new PropertyMetadata(new ObservableCollection<WordCloudEntry>(), EntriesChanged));

        public static readonly DependencyProperty LargestSizeWidthProportionProperty =
            DependencyProperty.Register("LargestSizeWidthProportion", typeof(double), typeof(WordCloud), new PropertyMetadata(0.50, LargestSizeWidthProportionChanged));

        public static readonly DependencyProperty MinFontSizeProperty =
            DependencyProperty.Register("MinFontSize", typeof(double), typeof(WordCloud), new PropertyMetadata(11.0, MinFontSizeChanged));

        public static readonly DependencyProperty FromColorProperty =
            DependencyProperty.Register("DefaultColor", typeof(SolidColorBrush), typeof(WordCloud), new PropertyMetadata(new SolidColorBrush(Colors.Black), FromColorChanged));

        public static readonly DependencyProperty MaxWordsProperty =
            DependencyProperty.Register("MaxWords", typeof(int), typeof(WordCloud), new PropertyMetadata(150, MaxWordsChanged));

        public static readonly DependencyProperty SelectedItemsProperty =
            DependencyProperty.Register("SelectedItems", typeof(ObservableCollection<int>), typeof(WordCloud), new PropertyMetadata(new ObservableCollection<int>(), SelectedItemsChanged));

        public static readonly DependencyProperty SelectedColorProperty =
            DependencyProperty.Register("SelectedColor", typeof(Brush), typeof(WordCloud), new PropertyMetadata(new SolidColorBrush(Colors.White), SelectedColorChanged));

        public static readonly DependencyProperty FromAlphaProperty =
            DependencyProperty.Register("FromAlpha", typeof(int), typeof(WordCloud), new PropertyMetadata(0x40, FromAlphaChanged));

        public static readonly DependencyProperty ToAlphaProperty =
            DependencyProperty.Register("ToAlpha", typeof(int), typeof(WordCloud), new PropertyMetadata(0xEE, ToAlphaChanged));

        public static readonly DependencyProperty AngleCenterValueProperty =
            DependencyProperty.Register("AngleCenterValue", typeof(double), typeof(WordCloud), new PropertyMetadata(double.NaN, AngleCenterValueChanged));

        public static readonly DependencyProperty MinimumLargestAngleValueProperty =
            DependencyProperty.Register("MinimumLargestAngleValue", typeof(double), typeof(WordCloud), new PropertyMetadata(double.NaN, MinimumLargestAngleValueChanged));

        public static readonly DependencyProperty MaximumLowestAngleValueProperty =
            DependencyProperty.Register("MaximumLowestAngleValue", typeof(double), typeof(WordCloud), new PropertyMetadata(double.NaN, MaximumLowestAngleValueChanged));


        private readonly DispatcherTimer _timer = new DispatcherTimer
                                                      {
                                                          Interval = TimeSpan.FromMilliseconds(100)
                                                      };

        private int _delay;

        private Image _image;
        private Grid _layoutRoot;
        private int[] _mapping;
        private double _minimumLargestValue = 6;
        private double _minimumValue = double.NaN;
        private Random _random;
        private WriteableBitmap _source;

        private INotifyCollectionChanged _entries;
        private INotifyCollectionChanged _selected;



        public WordCloud()
        {
            DefaultStyleKey = typeof(WordCloud);
            _timer.Tick += TimerTick;
            OnEntriesChanged(new DependencyPropertyChangedEventArgs());
            OnSelectedItemsChanged(new DependencyPropertyChangedEventArgs());
        }

        public double AngleCenterValue
        {
            get { return (double)GetValue(AngleCenterValueProperty); }
            set { SetValue(AngleCenterValueProperty, value); }
        }

        public ObservableCollection<WordCloudEntry> Entries
        {
            get { return (ObservableCollection<WordCloudEntry>)GetValue(EntriesProperty); }
            set { SetValue(EntriesProperty, value); }
        }


        public ObservableCollection<int> SelectedItems
        {
            get { return (ObservableCollection<int>)GetValue(SelectedItemsProperty); }
            set { SetValue(SelectedItemsProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SelectedItems.  This enables animation, styling, binding, etc...


        public double LargestSizeWidthProportion
        {
            get { return (double)GetValue(LargestSizeWidthProportionProperty); }
            set { SetValue(LargestSizeWidthProportionProperty, value); }
        }


        public double MinFontSize
        {
            get { return (double)GetValue(MinFontSizeProperty); }
            set { SetValue(MinFontSizeProperty, value); }
        }

        public SolidColorBrush DefaultColor
        {
            get { return (SolidColorBrush)GetValue(FromColorProperty); }
            set { SetValue(FromColorProperty, value); }
        }

        public int MaxWords
        {
            get { return (int)GetValue(MaxWordsProperty); }
            set { SetValue(MaxWordsProperty, value); }
        }

        public Brush SelectedColor
        {
            get { return (Brush)GetValue(SelectedColorProperty); }
            set { SetValue(SelectedColorProperty, value); }
        }

        public int FromAlpha
        {
            get { return (int)GetValue(FromAlphaProperty); }
            set { SetValue(FromAlphaProperty, value); }
        }

        public int ToAlpha
        {
            get { return (int)GetValue(ToAlphaProperty); }
            set { SetValue(ToAlphaProperty, value); }
        }

        public double MinimumValue
        {
            get { return _minimumValue; }
            set
            {
                _minimumValue = value;
                OnPropertyChanged("MinimumValue");
            }
        }

        public double MinimumLargestValue
        {
            get { return _minimumLargestValue; }
            set
            {
                _minimumLargestValue = value;
                OnPropertyChanged("MinimumLargestValue");
            }
        }


        public double MinimumLargestAngleValue
        {
            get { return (double)GetValue(MinimumLargestAngleValueProperty); }
            set { SetValue(MinimumLargestAngleValueProperty, value); }
        }

        public double MaximumLowestAngleValue
        {
            get { return (double)GetValue(MaximumLowestAngleValueProperty); }
            set { SetValue(MaximumLowestAngleValueProperty, value); }
        }

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        // Using a DependencyProperty as the backing store for MinimumLargestAngleValue.  This enables animation, styling, binding, etc...

        private static void MinimumLargestAngleValueChanged(
            object sender,
            DependencyPropertyChangedEventArgs e)
        {
            var owner = (WordCloud)sender;
            owner.OnMinimumLargestAngleValueChanged(e);
        }

        protected void OnMinimumLargestAngleValueChanged(DependencyPropertyChangedEventArgs e)
        {
            RegenerateCloud();
        }


        private static void MaximumLowestAngleValueChanged(
            object sender,
            DependencyPropertyChangedEventArgs e)
        {
            var owner = (WordCloud)sender;
            owner.OnMaximumLowestAngleValueChanged(e);
        }

        protected void OnMaximumLowestAngleValueChanged(DependencyPropertyChangedEventArgs e)
        {
            RegenerateCloud();
        }

        private static void AngleCenterValueChanged(
            object sender,
            DependencyPropertyChangedEventArgs e)
        {
            var owner = (WordCloud)sender;
            owner.OnAngleCenterValueChanged(e);
        }

        protected void OnAngleCenterValueChanged(DependencyPropertyChangedEventArgs e)
        {
            RegenerateCloud();
        }

        private void TimerTick(object sender, EventArgs e)
        {
            if (--_delay < 0)
            {
                _timer.Stop();
                InternalRegenerateCloud();
            }
        }

        private static void SelectedItemsChanged(
            object sender,
            DependencyPropertyChangedEventArgs e)
        {
            var owner = (WordCloud)sender;
            owner.OnSelectedItemsChanged(e);
        }

        protected void OnSelectedItemsChanged(DependencyPropertyChangedEventArgs e)
        {
            if (_selected != null)
            {
                _selected.CollectionChanged -= EntriesCollectionChanged;
            }

            _selected = SelectedItems;
            _selected.CollectionChanged += EntriesCollectionChanged;

            RegenerateCloud();
        }

        private static void EntriesChanged(
            object sender,
            DependencyPropertyChangedEventArgs e)
        {
            var owner = (WordCloud)sender;
            owner.OnEntriesChanged(e);
        }

        protected void OnEntriesChanged(DependencyPropertyChangedEventArgs e)
        {
            if (_entries != null)
            {
                _entries.CollectionChanged -= EntriesCollectionChanged;
            }

            _entries = Entries;
            _entries.CollectionChanged += EntriesCollectionChanged;

            RegenerateCloud();
        }

        void EntriesCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            RegenerateCloud();
        }


        private static void SelectedColorChanged(
            object sender,
            DependencyPropertyChangedEventArgs e)
        {
            var owner = (WordCloud)sender;
            owner.OnSelectedColorChanged(e);
        }

        protected void OnSelectedColorChanged(DependencyPropertyChangedEventArgs e)
        {
            RegenerateCloud();
        }


        public WordCloudEntry GetEntry(Point pt)
        {
            if (pt.X < 0 || pt.X >= _source.PixelWidth || pt.Y < 0 || pt.Y >= _source.PixelHeight)
                return null;
            int idx = _mapping[((int)(pt.Y / 4) * (_source.PixelWidth / 4)) + (int)pt.X / 4];
            return idx == -1 ? null : Entries[idx];
        }


        private static void LargestSizeWidthProportionChanged(
            object sender,
            DependencyPropertyChangedEventArgs e)
        {
            var owner = (WordCloud)sender;
            owner.OnLargestSizeWidthProportionChanged(e);
        }

        protected void OnLargestSizeWidthProportionChanged(DependencyPropertyChangedEventArgs e)
        {
            RegenerateCloud();
        }

        private static void MinFontSizeChanged(
            object sender,
            DependencyPropertyChangedEventArgs e)
        {
            var owner = (WordCloud)sender;
            owner.OnMinFontSizeChanged(e);
        }

        protected void OnMinFontSizeChanged(DependencyPropertyChangedEventArgs e)
        {
            RegenerateCloud();
        }


        protected void OnLargestSizeFactorChanged(DependencyPropertyChangedEventArgs e)
        {
            RegenerateCloud();
        }


        private static void ToColorChanged(
            object sender,
            DependencyPropertyChangedEventArgs e)
        {
            var owner = (WordCloud)sender;
            owner.OnToColorChanged(e);
        }

        protected void OnToColorChanged(DependencyPropertyChangedEventArgs e)
        {
            RegenerateCloud();
        }


        private static void FromColorChanged(
            object sender,
            DependencyPropertyChangedEventArgs e)
        {
            var owner = (WordCloud)sender;
            owner.OnFromColorChanged(e);
        }

        protected void OnFromColorChanged(DependencyPropertyChangedEventArgs e)
        {
            RegenerateCloud();
        }


        private static void MaxWordsChanged(
            object sender,
            DependencyPropertyChangedEventArgs e)
        {
            var owner = (WordCloud)sender;
            owner.OnMaxWordsChanged(e);
        }

        protected void OnMaxWordsChanged(DependencyPropertyChangedEventArgs e)
        {
            RegenerateCloud();
        }


        private static void FromAlphaChanged(
            object sender,
            DependencyPropertyChangedEventArgs e)
        {
            var owner = (WordCloud)sender;
            owner.OnFromAlphaChanged(e);
        }

        protected void OnFromAlphaChanged(DependencyPropertyChangedEventArgs e)
        {
            RegenerateCloud();
        }


        private static void ToAlphaChanged(
            object sender,
            DependencyPropertyChangedEventArgs e)
        {
            var owner = (WordCloud)sender;
            owner.OnToAlphaChanged(e);
        }

        protected void OnToAlphaChanged(DependencyPropertyChangedEventArgs e)
        {
            RegenerateCloud();
        }


        private Brush GetInterpolatedBrush(double value, Color color = default(Color))
        {
            if (color == default(Color))
                color = DefaultColor.Color;
            return new SolidColorBrush(Color.FromArgb((byte)(FromAlpha + (int)(value * (ToAlpha - FromAlpha))),
                                                      color.R,
                                                      color.G,
                                                      color.B));
        }

        public void RegenerateCloud()
        {
            if (_layoutRoot == null || _layoutRoot.ActualWidth <= 1)
                return;

            _delay = 2;
            _timer.Start();
        }

        public void InternalRegenerateCloud()
        {
            _source = new WriteableBitmap((int)_layoutRoot.ActualWidth, (int)_layoutRoot.ActualHeight);
            int arraySize = (int)((_layoutRoot.ActualWidth / 4) + 2) * (int)((_layoutRoot.ActualHeight / 4) + 2);
            _mapping = new int[arraySize];
            for (int i = 0; i < arraySize; i++) _mapping[i] = -1;

            if (Entries.Count() < 2)
            {
                _image.Source = _source;
                _image.InvalidateArrange();
                return;
            }

            _random = new Random(10202);

            double minSize = Entries.OrderByDescending(e => e.SizeValue).Take(MaxWords).Min(e => e.SizeValue);
            if (!double.IsNaN(MinimumValue))
                minSize = Math.Min(MinimumValue, minSize);
            double maxSize = Math.Max(Entries.Max(e => e.SizeValue), MinimumLargestValue);
            double range = Math.Max(0.00001, maxSize - minSize);
            double minColor = Entries.OrderByDescending(e => e.SizeValue).Take(MaxWords).Min(e => e.ColorValue);
            double maxColor = Entries.OrderByDescending(e => e.SizeValue).Take(MaxWords).Max(e => e.ColorValue);
            double maxAngle = Entries.OrderByDescending(e => e.SizeValue).Take(MaxWords).Max(e => e.Angle);
            if (!double.IsNaN(MinimumLargestAngleValue))
                maxAngle = Math.Max(MinimumLargestAngleValue, maxAngle);
            double minAngle = Entries.OrderByDescending(e => e.SizeValue).Take(MaxWords).Min(e => e.Angle);
            if (!double.IsNaN(MaximumLowestAngleValue))
                minAngle = Math.Min(MaximumLowestAngleValue, minAngle);


            double colorRange = Math.Max(0, maxColor - minColor);

            double angleRange = Math.Max(0, maxAngle - minAngle);
            //If there's a centre value then specify the range
            if (!double.IsNaN(AngleCenterValue))
            {
                var lr = AngleCenterValue - minAngle;
                var ur = maxAngle - AngleCenterValue;
                angleRange = Math.Max(ur, lr);
            }


            var txt = new TextBlock
                          {
                              FontFamily = FontFamily,
                              FontSize = 100,
                              Text = "x"
                          };


            double areaPerLetter = ((txt.ActualWidth)) / (range);

            double targetWidth = _layoutRoot.ActualWidth * LargestSizeWidthProportion;
            WordCloudEntry od = Entries.OrderByDescending(e => (e.SizeValue - minSize) * e.Word.Length).First();
            double maxFontSize = Math.Max(MinFontSize * 2.7, 100 / (((od.SizeValue - minSize) * od.Word.Length * areaPerLetter) / targetWidth));
            double fontMultiplier = Math.Min((maxFontSize - MinFontSize) / range, 200);

            var points = new[]
                             {
                                 new Point((int) (_layoutRoot.ActualWidth/2), (int) (_layoutRoot.ActualHeight/2)),
                                 new Point((int) (_layoutRoot.ActualWidth/4), (int) (_layoutRoot.ActualHeight/4)),
                                 new Point((int) (_layoutRoot.ActualWidth/4), (int) (3*_layoutRoot.ActualHeight/2)),
                                 new Point((int) (3*_layoutRoot.ActualWidth/4), (int) (_layoutRoot.ActualHeight/2)),
                                 new Point((int) (3*_layoutRoot.ActualWidth/4), (int) (3*_layoutRoot.ActualHeight/4))
                             };


            int currentPoint = 0;
            foreach (WordCloudEntry e in Entries.OrderByDescending(e => e.SizeValue).Take(MaxWords))
            {
            again:
                double position = 0.0;
                Point centre = points[currentPoint];

                double angle = 0.0;
                if (double.IsNaN(AngleCenterValue))
                {
                    angle = angleRange >= 0.01 ? -90 + (((e.Angle - minAngle) / angleRange) * 90) : 0;
                }
                else
                {
                    angle = angleRange >= 0.01 ? 90 * ((e.Angle - AngleCenterValue) / angleRange) : 0;
                }
                WriteableBitmap bm = CreateImage(e.Word,
                                                 ((e.SizeValue - minSize) * fontMultiplier) + MinFontSize,
                                                 SelectedItems.Contains(Entries.IndexOf(e)) ? -1 : (colorRange >= 0.01 ? (e.ColorValue - minColor) / colorRange : 1), e.Color,
                                                 angle);
                Dictionary<Point, List<Point>> lst = CreateCollisionList(bm);
                bool collided = true;
                do
                {
                    Point spiralPoint = GetSpiralPoint(position);
                    int offsetX = (bm.PixelWidth / 2);
                    int offsetY = (bm.PixelHeight / 2);
                    var testPoint = new Point((int)(spiralPoint.X + centre.X - offsetX), (int)(spiralPoint.Y + centre.Y - offsetY));
                    if (position > (2 * Math.PI) * 580)
                    {
                        if (++currentPoint >= points.Length)
                            goto done;
                        goto again;
                    }
                    int cols = CountCollisions(testPoint, lst);
                    if (cols == 0)
                    {
                    tryagain:
                        double oldY = testPoint.Y;
                        if (Math.Abs(testPoint.X + offsetX - centre.X) > 10)
                        {
                            if (testPoint.X + offsetX < centre.X)
                            {
                                do
                                {
                                    testPoint.X += 2;
                                } while (testPoint.X + offsetX < centre.X && CountCollisions(testPoint, lst) == 0);
                                testPoint.X -= 2;
                            }
                            else
                            {
                                do
                                {
                                    testPoint.X -= 2;
                                } while (testPoint.X + offsetX > centre.X && CountCollisions(testPoint, lst) == 0);
                                testPoint.X += 2;
                            }
                        }
                        if (Math.Abs(testPoint.Y + offsetY - centre.Y) > 10)
                        {
                            if (testPoint.Y + offsetY < centre.Y)
                            {
                                do
                                {
                                    testPoint.Y += 2;
                                } while (testPoint.Y + offsetY < centre.Y && CountCollisions(testPoint, lst) == 0);
                                testPoint.Y -= 2;
                            }
                            else
                            {
                                do
                                {
                                    testPoint.Y -= 2;
                                } while (testPoint.Y + offsetY > centre.Y && CountCollisions(testPoint, lst) == 0);
                                testPoint.Y += 2;
                            }
                            if (testPoint.Y != oldY)
                                goto tryagain;
                        }


                        collided = false;
                        CopyBits(testPoint, bm, lst, Entries.IndexOf(e));
                    }
                    else
                    {
                        if (cols <= 2)
                        {
                            position += (2 * Math.PI) / 100;
                        }
                        else

                            position += (2 * Math.PI) / 40;
                    }
                } while (collided);
            }
        done:
            _image.Source = _source;
            _image.InvalidateArrange();
        }

        private int CountCollisions(Point testPoint, Dictionary<Point, List<Point>> lst)
        {
            int testRight = GetCollisions(new Point(testPoint.X + 2, testPoint.Y), lst);
            int testLeft = GetCollisions(new Point(testPoint.X - 2, testPoint.Y), lst);
            int cols = GetCollisions(testPoint, lst) + testRight + testLeft + GetCollisions(new Point(testPoint.X, testPoint.Y + 2), lst) + GetCollisions(new Point(testPoint.X, testPoint.Y - 2), lst);
            return cols;
        }


        //Property MinimumValue


        private void CopyBits(Point testPoint, WriteableBitmap bm, Dictionary<Point, List<Point>> lst, int index)
        {
            int pixelWidth = _source.PixelWidth;
            int mapWidth = pixelWidth / 4;
            int width = bm.PixelWidth;

            foreach (Point pt in lst.SelectMany(e => e.Value))
            {
                int[] pixels = _source.Pixels;
                int[] sourcePixels = bm.Pixels;
                if ((pt.X + testPoint.X) >= 0 && (pt.X + testPoint.X) < _source.PixelWidth && (pt.Y + testPoint.Y) >= 0 && (pt.Y + testPoint.Y) < _source.PixelHeight)
                {
                    pixels[(int)((testPoint.Y + pt.Y) * pixelWidth) + (int)(pt.X + testPoint.X)] = sourcePixels[(int)(pt.Y * width) + (int)pt.X];
                }
            }
            int sx = (int)testPoint.X / 4;
            int sy = (int)testPoint.Y / 4;
            foreach (Point pt in lst.Select(e => e.Key))
            {
                _mapping[(int)(pt.Y + sy) * mapWidth + (int)(pt.X + sx)] = index;
                _mapping[(int)(pt.Y + sy + 1) * mapWidth + (int)(pt.X + sx)] = index;
                _mapping[(int)(pt.Y + sy + 1) * mapWidth + (int)(pt.X + 1 + sx)] = index;
                _mapping[(int)(pt.Y + sy) * mapWidth + (int)(pt.X + 1 + sx)] = index;
            }
        }

        private Point GetSpiralPoint(double position, double radius = 7)
        {
            double mult = position / (2 * Math.PI) * radius;
            double angle = position % (2 * Math.PI);
            return new Point((int)(mult * Math.Sin(angle)), (int)(mult * Math.Cos(angle)));
        }

        private WriteableBitmap CreateImage(string text, double size = 100, double colorValue = 1, Color wordColor = default(Color), double angle = 0)
        {
            if (text == string.Empty)
                return new WriteableBitmap(0, 0);
            var txt = new TextBlock
                          {
                              FontFamily = FontFamily,
                              FontSize = Math.Max(size, MinFontSize),
                              Text = text,
                              Foreground = colorValue >= 0 ? GetInterpolatedBrush(colorValue, wordColor) : SelectedColor
                          };
            //   txt.Effect = new DropShadowEffect() { ShadowDepth = 2, BlurRadius = 4, Color = Colors.Red, Direction = 0 };

            double largest = Math.Max(Math.Ceiling(txt.ActualHeight), Math.Ceiling(txt.ActualWidth)) * 1.2;
            var bm = new WriteableBitmap((int)Math.Ceiling(largest), (int)Math.Ceiling(largest));

            var tfg = new TransformGroup();
            var rot = new RotateTransform
                          {
                              Angle = angle,
                              CenterX = 0.5,
                              CenterY = 0.5
                          };
            tfg.Children.Add(rot);

            var comp = new CompositeTransform
                           {
                               Rotation = angle,
                               CenterX = txt.ActualWidth / 2,
                               CenterY = txt.ActualHeight / 2,
                               TranslateY = largest > txt.ActualHeight ? ((largest - txt.ActualHeight) / 2) : 0,
                               TranslateX = largest > txt.ActualWidth ? ((largest - txt.ActualWidth) / 2) : 0
                           };

            bm.Render(txt, comp);
            bm.Invalidate();


            return bm;
        }


        private Dictionary<Point, List<Point>> CreateCollisionList(WriteableBitmap bmp)
        {
            var l = new List<Point>();
            int pixelHeight = bmp.PixelHeight;
            var lookup = new Dictionary<Point, List<Point>>();

            for (int y = 0; y < pixelHeight; y++)
            {
                int pixelWidth = bmp.PixelWidth;
                for (int x = 0; x < pixelWidth; x++)
                {
                    int[] pixels = bmp.Pixels;
                    if (pixels[y * pixelWidth + x] != 0)
                    {
                        var detailPoint = new Point(x, y);
                        l.Add(detailPoint);
                        var blockPoint = new Point(((x / 4)), ((y / 4)));
                        if (!lookup.ContainsKey(blockPoint))
                        {
                            lookup[blockPoint] = new List<Point>();
                        }
                        lookup[blockPoint].Add(detailPoint);
                    }
                }
            }
            return lookup;
        }

        private int GetCollisions(Point pt, Dictionary<Point, List<Point>> list)
        {
            int[] pixels = _source.Pixels;
            int pixelWidth = _source.PixelWidth;
            int mapWidth = (_source.PixelWidth / 4);

            int c = 0;
            foreach (var pair in list)
            {
                var testPt = new Point(pt.X + pair.Key.X * 4, pt.Y + pair.Key.Y * 4);
                if (testPt.X < 0 || testPt.X >= _source.PixelWidth || testPt.Y < 0 || testPt.Y >= _source.PixelHeight)
                    return 1;
                int pos = ((((int)pair.Key.Y + (int)(pt.Y / 4)) * mapWidth) + (int)pair.Key.X + ((int)(pt.X / 4)));
                try
                {
                    if (_mapping[pos] != -1 || _mapping[pos + 1] != -1 || _mapping[pos + mapWidth] != -1 || _mapping[pos + mapWidth + 1] != -1)
                    {
                        foreach (Point p in pair.Value)
                        {
                            var nx = (int)(p.X + pt.X);
                            var ny = (int)(pt.Y + p.Y);
                            if (nx < 0 || nx >= _source.PixelWidth || ny < 0 || ny >= _source.PixelHeight)
                                return 1;
                            if (pixels[ny * pixelWidth + nx] != 0) return 1;
                        }
                    }
                }
                catch (Exception)
                {
                    return 1;
                }
            }
            return 0;
        }

        private static void CloudWidthChanged(
            object sender,
            DependencyPropertyChangedEventArgs e)
        {
            var owner = (WordCloud)sender;
            owner.OnCloudWidthChanged(e);
        }

        protected void OnCloudWidthChanged(DependencyPropertyChangedEventArgs e)
        {
            RegenerateCloud();
        }


        public override void OnApplyTemplate()
        {
            _image = GetTemplateChild("Image") as Image ?? new Image();
            _layoutRoot = GetTemplateChild("LayoutRoot") as Grid ?? new Grid();
            _layoutRoot.SizeChanged += LayoutRootSizeChanged;
            RegenerateCloud();
            base.OnApplyTemplate();
        }

        private void LayoutRootSizeChanged(object sender, SizeChangedEventArgs e)
        {
            RegenerateCloud();
            OnPropertyChanged("WordCloudImage");
        }


        public void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(name));
        }

        #region Nested type: WordCloudEntry

        public class WordCloudEntry
        {
            public string Word { get; set; }
            public double SizeValue { get; set; }
            public double ColorValue { get; set; }
            public object Tag { get; set; }
            public double Angle { get; set; }
            public Color Color { get; set; }
        }

        #endregion
    }
}