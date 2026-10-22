using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace AirControls;

public class AirMeter : UserControl
{
	private decimal barValue;

	private decimal barRange = 100m;

	private int barMaximum = 100;

	private int barMinimum;

	private decimal tickMark = -1m;

	private Color _borderColor = Color.LightGray;

	private Color _barColor = Color.Lime;

	private int barStyle;

	private int channel;

	public bool FineMode;

	private decimal[] faderMap = new decimal[1024]
	{
		0.000m, 1.000m, 2.956m, 4.912m, 6.868m, 8.825m, 10.781m, 12.737m, 14.693m, 16.649m,
		18.605m, 20.562m, 22.518m, 24.474m, 26.430m, 28.386m, 29.038m, 29.690m, 30.342m, 30.994m,
		31.646m, 32.298m, 32.951m, 33.603m, 34.255m, 34.907m, 35.559m, 36.211m, 36.863m, 37.515m,
		38.167m, 38.819m, 39.471m, 40.123m, 40.253m, 40.384m, 40.514m, 40.645m, 40.775m, 40.906m,
		41.036m, 41.166m, 41.297m, 41.427m, 41.558m, 41.688m, 41.818m, 41.949m, 42.079m, 42.210m,
		42.340m, 42.470m, 42.601m, 42.731m, 42.862m, 42.992m, 43.123m, 43.253m, 43.383m, 43.514m,
		43.644m, 43.775m, 43.905m, 44.035m, 44.166m, 44.296m, 44.427m, 44.557m, 44.687m, 44.818m,
		44.948m, 45.079m, 45.209m, 45.339m, 45.470m, 45.600m, 45.731m, 45.861m, 45.992m, 46.122m,
		46.252m, 46.383m, 46.513m, 46.644m, 46.774m, 46.904m, 47.035m, 47.165m, 47.296m, 47.426m,
		47.556m, 47.687m, 47.817m, 47.948m, 48.078m, 48.209m, 48.339m, 48.469m, 48.600m, 48.730m,
		48.861m, 48.991m, 49.121m, 49.252m, 49.382m, 49.513m, 49.643m, 49.773m, 49.904m, 50.034m,
		50.165m, 50.295m, 50.425m, 50.556m, 50.686m, 50.817m, 50.947m, 51.078m, 51.208m, 51.338m,
		51.469m, 51.599m, 51.730m, 51.860m, 52.848m, 53.836m, 54.824m, 55.812m, 56.800m, 57.787m,
		58.775m, 59.763m, 60.751m, 61.739m, 62.727m, 63.715m, 64.703m, 65.691m, 66.679m, 67.666m,
		68.654m, 69.642m, 70.630m, 71.618m, 72.606m, 73.594m, 74.582m, 75.570m, 76.558m, 77.545m,
		78.533m, 79.521m, 80.509m, 81.497m, 82.485m, 83.473m, 84.461m, 85.449m, 86.437m, 87.424m,
		88.412m, 89.400m, 90.388m, 91.376m, 92.364m, 93.352m, 94.340m, 95.328m, 96.316m, 97.303m,
		98.291m, 99.279m, 100.267m, 101.255m, 102.428m, 103.601m, 104.774m, 105.946m, 107.119m, 108.292m,
		109.465m, 110.638m, 111.811m, 112.984m, 114.156m, 115.329m, 116.502m, 117.675m, 118.848m, 120.021m,
		121.194m, 122.366m, 123.539m, 124.712m, 125.885m, 127.058m, 128.231m, 129.404m, 130.577m, 131.749m,
		132.922m, 134.095m, 135.268m, 136.441m, 137.614m, 138.787m, 139.959m, 141.132m, 142.305m, 143.478m,
		144.651m, 145.824m, 146.997m, 148.169m, 149.342m, 150.515m, 151.688m, 152.861m, 154.034m, 155.207m,
		156.379m, 157.552m, 158.725m, 159.898m, 161.136m, 162.374m, 163.612m, 164.850m, 166.088m, 167.326m,
		168.564m, 169.802m, 171.040m, 172.278m, 173.516m, 174.754m, 175.992m, 177.230m, 178.468m, 179.706m,
		180.944m, 182.182m, 183.420m, 184.658m, 185.896m, 187.134m, 188.372m, 189.610m, 190.849m, 192.087m,
		193.325m, 194.563m, 195.801m, 197.039m, 198.277m, 199.515m, 200.753m, 201.991m, 203.229m, 204.467m,
		205.705m, 206.943m, 208.181m, 209.419m, 210.657m, 211.895m, 213.133m, 214.371m, 215.609m, 216.847m,
		218.085m, 219.323m, 220.561m, 221.799m, 222.972m, 224.145m, 225.318m, 226.491m, 227.663m, 228.836m,
		230.009m, 231.182m, 232.355m, 233.528m, 234.701m, 235.874m, 237.046m, 238.219m, 239.392m, 240.565m,
		241.738m, 242.911m, 244.084m, 245.257m, 246.429m, 247.602m, 248.775m, 249.948m, 251.121m, 252.294m,
		253.467m, 254.640m, 255.813m, 256.985m, 258.158m, 259.331m, 260.504m, 261.677m, 262.850m, 264.023m,
		265.196m, 266.368m, 267.541m, 268.714m, 269.887m, 271.060m, 272.233m, 273.406m, 274.579m, 275.751m,
		276.924m, 278.097m, 279.270m, 280.443m, 281.681m, 282.919m, 284.157m, 285.395m, 286.633m, 287.871m,
		289.109m, 290.347m, 291.585m, 292.823m, 294.061m, 295.299m, 296.537m, 297.775m, 299.013m, 300.251m,
		301.489m, 302.727m, 303.965m, 305.203m, 306.441m, 307.679m, 308.917m, 310.155m, 311.394m, 312.632m,
		313.870m, 315.108m, 316.346m, 317.584m, 318.822m, 320.060m, 321.298m, 322.536m, 323.774m, 325.012m,
		326.250m, 327.488m, 328.726m, 329.964m, 331.202m, 332.440m, 333.678m, 334.916m, 336.154m, 337.392m,
		338.630m, 339.868m, 341.106m, 342.344m, 343.512m, 344.679m, 345.847m, 347.015m, 348.183m, 349.350m,
		350.518m, 351.686m, 352.853m, 354.021m, 355.189m, 356.356m, 357.524m, 358.692m, 359.860m, 361.027m,
		362.195m, 363.363m, 364.530m, 365.698m, 366.866m, 368.033m, 369.201m, 370.369m, 371.537m, 372.704m,
		373.872m, 375.040m, 376.207m, 377.375m, 378.543m, 379.710m, 380.878m, 382.046m, 383.214m, 384.381m,
		385.549m, 386.717m, 387.884m, 389.052m, 390.220m, 391.387m, 392.555m, 393.723m, 394.891m, 396.058m,
		397.226m, 398.394m, 399.561m, 400.729m, 401.343m, 401.957m, 402.571m, 403.184m, 403.798m, 404.412m,
		405.026m, 405.640m, 406.254m, 406.868m, 407.481m, 408.095m, 408.709m, 409.323m, 409.937m, 410.551m,
		411.165m, 411.778m, 412.392m, 413.006m, 413.620m, 414.234m, 414.848m, 415.462m, 416.076m, 416.689m,
		417.303m, 417.917m, 418.531m, 419.145m, 419.759m, 420.373m, 420.986m, 421.600m, 422.214m, 422.828m,
		423.442m, 424.056m, 424.670m, 425.283m, 425.897m, 426.511m, 427.125m, 427.739m, 428.353m, 428.967m,
		429.580m, 430.194m, 430.808m, 431.422m, 432.036m, 432.650m, 433.264m, 433.877m, 434.491m, 435.105m,
		435.719m, 436.333m, 436.947m, 437.561m, 438.174m, 438.788m, 439.402m, 440.016m, 440.630m, 441.244m,
		441.858m, 442.471m, 443.085m, 443.699m, 444.313m, 444.927m, 445.541m, 446.155m, 446.769m, 447.382m,
		447.996m, 448.610m, 449.224m, 449.838m, 450.452m, 451.066m, 451.679m, 452.293m, 452.907m, 453.521m,
		454.135m, 454.749m, 455.363m, 455.976m, 456.590m, 457.204m, 457.818m, 458.432m, 459.046m, 459.660m,
		460.273m, 460.887m, 461.501m, 462.115m, 462.702m, 463.289m, 463.875m, 464.462m, 465.049m, 465.636m,
		466.222m, 466.809m, 467.396m, 467.983m, 468.569m, 469.156m, 469.743m, 470.330m, 470.916m, 471.503m,
		472.090m, 472.677m, 473.263m, 473.850m, 474.437m, 475.024m, 475.610m, 476.197m, 476.784m, 477.371m,
		477.957m, 478.544m, 479.131m, 479.718m, 480.304m, 480.891m, 481.478m, 482.065m, 482.651m, 483.238m,
		483.825m, 484.412m, 484.998m, 485.585m, 486.172m, 486.759m, 487.345m, 487.932m, 488.519m, 489.106m,
		489.692m, 490.279m, 490.866m, 491.453m, 492.039m, 492.626m, 493.213m, 493.800m, 494.386m, 494.973m,
		495.560m, 496.147m, 496.733m, 497.320m, 497.907m, 498.494m, 499.080m, 499.667m, 500.254m, 500.841m,
		501.427m, 502.014m, 502.601m, 503.188m, 503.774m, 504.361m, 504.948m, 505.535m, 506.121m, 506.708m,
		507.295m, 507.882m, 508.468m, 509.055m, 509.642m, 510.229m, 510.815m, 511.402m, 511.989m, 512.576m,
		513.162m, 513.749m, 514.336m, 514.923m, 515.509m, 516.096m, 516.683m, 517.270m, 517.856m, 518.443m,
		519.030m, 519.617m, 520.203m, 520.790m, 522.033m, 523.275m, 524.518m, 525.760m, 527.003m, 528.245m,
		529.488m, 530.730m, 531.973m, 533.215m, 534.458m, 535.700m, 536.943m, 538.186m, 539.428m, 540.671m,
		541.913m, 543.156m, 544.398m, 545.641m, 546.883m, 548.126m, 549.368m, 550.611m, 551.854m, 553.096m,
		554.339m, 555.581m, 556.824m, 558.066m, 559.309m, 560.551m, 561.794m, 563.036m, 564.279m, 565.521m,
		566.764m, 568.007m, 569.249m, 570.492m, 571.734m, 572.977m, 574.219m, 575.462m, 576.704m, 577.947m,
		579.189m, 580.432m, 581.674m, 582.917m, 584.025m, 585.132m, 586.240m, 587.348m, 588.456m, 589.563m,
		590.671m, 591.779m, 592.886m, 593.994m, 595.102m, 596.210m, 597.317m, 598.425m, 599.533m, 600.641m,
		601.748m, 602.856m, 603.964m, 605.071m, 606.179m, 607.287m, 608.395m, 609.502m, 610.610m, 611.718m,
		612.825m, 613.933m, 615.041m, 616.149m, 617.256m, 618.364m, 619.472m, 620.579m, 621.687m, 622.795m,
		623.903m, 625.010m, 626.118m, 627.226m, 628.334m, 629.441m, 630.549m, 631.657m, 632.764m, 633.872m,
		634.980m, 636.088m, 637.195m, 638.303m, 639.606m, 640.909m, 642.213m, 643.516m, 644.819m, 646.122m,
		647.425m, 648.728m, 650.032m, 651.335m, 652.638m, 653.941m, 655.244m, 656.548m, 657.851m, 659.154m,
		660.457m, 661.760m, 663.063m, 664.367m, 665.670m, 666.973m, 668.276m, 669.579m, 670.883m, 672.186m,
		673.489m, 674.792m, 676.095m, 677.398m, 678.702m, 680.005m, 681.308m, 682.611m, 683.914m, 685.217m,
		686.521m, 687.824m, 689.127m, 690.430m, 691.733m, 693.037m, 694.340m, 695.643m, 696.946m, 698.249m,
		699.552m, 700.856m, 702.159m, 703.462m, 704.765m, 706.068m, 707.372m, 708.675m, 709.978m, 711.281m,
		712.584m, 713.887m, 715.191m, 716.494m, 717.797m, 719.100m, 720.403m, 721.707m, 723.010m, 724.313m,
		725.616m, 726.919m, 728.222m, 729.526m, 730.829m, 732.132m, 733.435m, 734.738m, 736.042m, 737.345m,
		738.648m, 739.951m, 741.254m, 742.557m, 743.861m, 745.164m, 746.467m, 747.770m, 749.073m, 750.376m,
		751.680m, 752.983m, 754.286m, 755.589m, 756.892m, 758.196m, 759.499m, 760.802m, 762.105m, 763.408m,
		764.711m, 766.015m, 767.318m, 768.621m, 769.869m, 771.117m, 772.365m, 773.613m, 774.861m, 776.109m,
		777.357m, 778.605m, 779.853m, 781.101m, 782.349m, 783.597m, 784.845m, 786.093m, 787.341m, 788.589m,
		789.838m, 791.086m, 792.334m, 793.582m, 794.830m, 796.078m, 797.326m, 798.574m, 799.822m, 801.070m,
		802.318m, 803.566m, 804.814m, 806.062m, 807.310m, 808.558m, 809.806m, 811.054m, 812.302m, 813.550m,
		814.798m, 816.046m, 817.294m, 818.542m, 819.790m, 821.038m, 822.286m, 823.534m, 824.782m, 826.030m,
		827.278m, 828.526m, 829.774m, 831.023m, 832.271m, 833.519m, 834.767m, 836.015m, 837.263m, 838.511m,
		839.759m, 841.007m, 842.255m, 843.503m, 844.751m, 845.999m, 847.247m, 848.495m, 849.743m, 850.991m,
		852.239m, 853.487m, 854.735m, 855.983m, 857.231m, 858.479m, 859.727m, 860.975m, 862.223m, 863.471m,
		864.719m, 865.967m, 867.215m, 868.463m, 869.711m, 870.959m, 872.207m, 873.456m, 874.704m, 875.952m,
		877.200m, 878.448m, 879.696m, 880.944m, 882.192m, 883.440m, 884.688m, 885.936m, 887.184m, 888.432m,
		889.680m, 890.928m, 892.176m, 893.424m, 894.592m, 895.761m, 896.929m, 898.097m, 899.266m, 900.434m,
		901.602m, 902.771m, 903.939m, 905.107m, 906.276m, 907.444m, 908.612m, 909.781m, 910.949m, 912.117m,
		913.286m, 914.454m, 915.622m, 916.791m, 917.959m, 919.127m, 920.296m, 921.464m, 922.633m, 923.801m,
		924.969m, 926.138m, 927.306m, 928.474m, 929.643m, 930.811m, 931.979m, 933.148m, 934.316m, 935.484m,
		936.653m, 937.821m, 938.989m, 940.158m, 941.326m, 942.494m, 943.663m, 944.831m, 945.999m, 947.168m,
		948.336m, 949.504m, 950.673m, 951.841m, 953.264m, 954.687m, 956.111m, 957.534m, 958.957m, 960.380m,
		961.803m, 963.226m, 964.650m, 966.073m, 967.496m, 968.919m, 970.342m, 971.766m, 973.189m, 974.612m,
		976.035m, 977.458m, 978.881m, 980.305m, 981.728m, 983.151m, 984.574m, 985.997m, 987.421m, 988.844m,
		990.267m, 991.690m, 993.113m, 994.536m, 995.960m, 997.383m, 998.806m, 1000.229m, 1001.652m, 1003.075m,
		1004.499m, 1005.922m, 1007.345m, 1008.768m, 1010.191m, 1011.615m, 1013.038m, 1014.461m, 1015.884m, 1017.307m,
		1018.730m, 1020.154m, 1021.577m, 1023.000m
	};

	private decimal[] dbMap = new decimal[1024]
	{
		-999.00m, -138.00m, -135.00m, -132.00m, -129.00m, -126.00m, -123.00m, -120.00m, -117.00m, -114.00m,
		-111.00m, -108.00m, -105.00m, -102.00m, -99.00m, -96.00m, -95.00m, -94.00m, -93.00m, -92.00m,
		-91.00m, -90.00m, -89.00m, -88.00m, -87.00m, -86.00m, -85.00m, -84.00m, -83.00m, -82.00m,
		-81.00m, -80.00m, -79.00m, -78.00m, -77.80m, -77.60m, -77.40m, -77.20m, -77.00m, -76.80m,
		-76.60m, -76.40m, -76.20m, -76.00m, -75.80m, -75.60m, -75.40m, -75.20m, -75.00m, -74.80m,
		-74.60m, -74.40m, -74.20m, -74.00m, -73.80m, -73.60m, -73.40m, -73.20m, -73.00m, -72.80m,
		-72.60m, -72.40m, -72.20m, -72.00m, -71.80m, -71.60m, -71.40m, -71.20m, -71.00m, -70.80m,
		-70.60m, -70.40m, -70.20m, -70.00m, -69.80m, -69.60m, -69.40m, -69.20m, -69.00m, -68.80m,
		-68.60m, -68.40m, -68.20m, -68.00m, -67.80m, -67.60m, -67.40m, -67.20m, -67.00m, -66.80m,
		-66.60m, -66.40m, -66.20m, -66.00m, -65.80m, -65.60m, -65.40m, -65.20m, -65.00m, -64.80m,
		-64.60m, -64.40m, -64.20m, -64.00m, -63.80m, -63.60m, -63.40m, -63.20m, -63.00m, -62.80m,
		-62.60m, -62.40m, -62.20m, -62.00m, -61.80m, -61.60m, -61.40m, -61.20m, -61.00m, -60.80m,
		-60.60m, -60.40m, -60.20m, -60.00m, -59.80m, -59.60m, -59.40m, -59.20m, -59.00m, -58.80m,
		-58.60m, -58.40m, -58.20m, -58.00m, -57.80m, -57.60m, -57.40m, -57.20m, -57.00m, -56.80m,
		-56.60m, -56.40m, -56.20m, -56.00m, -55.80m, -55.60m, -55.40m, -55.20m, -55.00m, -54.80m,
		-54.60m, -54.40m, -54.20m, -54.00m, -53.80m, -53.60m, -53.40m, -53.20m, -53.00m, -52.80m,
		-52.60m, -52.40m, -52.20m, -52.00m, -51.80m, -51.60m, -51.40m, -51.20m, -51.00m, -50.80m,
		-50.60m, -50.40m, -50.20m, -50.00m, -49.80m, -49.60m, -49.40m, -49.20m, -49.00m, -48.80m,
		-48.60m, -48.40m, -48.20m, -48.00m, -47.80m, -47.60m, -47.40m, -47.20m, -47.00m, -46.80m,
		-46.60m, -46.40m, -46.20m, -46.00m, -45.80m, -45.60m, -45.40m, -45.20m, -45.00m, -44.80m,
		-44.60m, -44.40m, -44.20m, -44.00m, -43.80m, -43.60m, -43.40m, -43.20m, -43.00m, -42.80m,
		-42.60m, -42.40m, -42.20m, -42.00m, -41.80m, -41.60m, -41.40m, -41.20m, -41.00m, -40.80m,
		-40.60m, -40.40m, -40.20m, -40.00m, -39.90m, -39.80m, -39.70m, -39.60m, -39.50m, -39.40m,
		-39.30m, -39.20m, -39.10m, -39.00m, -38.90m, -38.80m, -38.70m, -38.60m, -38.50m, -38.40m,
		-38.30m, -38.20m, -38.10m, -38.00m, -37.90m, -37.80m, -37.70m, -37.60m, -37.50m, -37.40m,
		-37.30m, -37.20m, -37.10m, -37.00m, -36.90m, -36.80m, -36.70m, -36.60m, -36.50m, -36.40m,
		-36.30m, -36.20m, -36.10m, -36.00m, -35.90m, -35.80m, -35.70m, -35.60m, -35.50m, -35.40m,
		-35.30m, -35.20m, -35.10m, -35.00m, -34.90m, -34.80m, -34.70m, -34.60m, -34.50m, -34.40m,
		-34.30m, -34.20m, -34.10m, -34.00m, -33.90m, -33.80m, -33.70m, -33.60m, -33.50m, -33.40m,
		-33.30m, -33.20m, -33.10m, -33.00m, -32.90m, -32.80m, -32.70m, -32.60m, -32.50m, -32.40m,
		-32.30m, -32.20m, -32.10m, -32.00m, -31.90m, -31.80m, -31.70m, -31.60m, -31.50m, -31.40m,
		-31.30m, -31.20m, -31.10m, -31.00m, -30.90m, -30.80m, -30.70m, -30.60m, -30.50m, -30.40m,
		-30.30m, -30.20m, -30.10m, -30.00m, -29.90m, -29.80m, -29.70m, -29.60m, -29.50m, -29.40m,
		-29.30m, -29.20m, -29.10m, -29.00m, -28.90m, -28.80m, -28.70m, -28.60m, -28.50m, -28.40m,
		-28.30m, -28.20m, -28.10m, -28.00m, -27.90m, -27.80m, -27.70m, -27.60m, -27.50m, -27.40m,
		-27.30m, -27.20m, -27.10m, -27.00m, -26.90m, -26.80m, -26.70m, -26.60m, -26.50m, -26.40m,
		-26.30m, -26.20m, -26.10m, -26.00m, -25.90m, -25.80m, -25.70m, -25.60m, -25.50m, -25.40m,
		-25.30m, -25.20m, -25.10m, -25.00m, -24.90m, -24.80m, -24.70m, -24.60m, -24.50m, -24.40m,
		-24.30m, -24.20m, -24.10m, -24.00m, -23.90m, -23.80m, -23.70m, -23.60m, -23.50m, -23.40m,
		-23.30m, -23.20m, -23.10m, -23.00m, -22.90m, -22.80m, -22.70m, -22.60m, -22.50m, -22.40m,
		-22.30m, -22.20m, -22.10m, -22.00m, -21.90m, -21.80m, -21.70m, -21.60m, -21.50m, -21.40m,
		-21.30m, -21.20m, -21.10m, -21.00m, -20.90m, -20.80m, -20.70m, -20.60m, -20.50m, -20.40m,
		-20.30m, -20.20m, -20.10m, -20.00m, -19.95m, -19.90m, -19.85m, -19.80m, -19.75m, -19.70m,
		-19.65m, -19.60m, -19.55m, -19.50m, -19.45m, -19.40m, -19.35m, -19.30m, -19.25m, -19.20m,
		-19.15m, -19.10m, -19.05m, -19.00m, -18.95m, -18.90m, -18.85m, -18.80m, -18.75m, -18.70m,
		-18.65m, -18.60m, -18.55m, -18.50m, -18.45m, -18.40m, -18.35m, -18.30m, -18.25m, -18.20m,
		-18.15m, -18.10m, -18.05m, -18.00m, -17.95m, -17.90m, -17.85m, -17.80m, -17.75m, -17.70m,
		-17.65m, -17.60m, -17.55m, -17.50m, -17.45m, -17.40m, -17.35m, -17.30m, -17.25m, -17.20m,
		-17.15m, -17.10m, -17.05m, -17.00m, -16.95m, -16.90m, -16.85m, -16.80m, -16.75m, -16.70m,
		-16.65m, -16.60m, -16.55m, -16.50m, -16.45m, -16.40m, -16.35m, -16.30m, -16.25m, -16.20m,
		-16.15m, -16.10m, -16.05m, -16.00m, -15.95m, -15.90m, -15.85m, -15.80m, -15.75m, -15.70m,
		-15.65m, -15.60m, -15.55m, -15.50m, -15.45m, -15.40m, -15.35m, -15.30m, -15.25m, -15.20m,
		-15.15m, -15.10m, -15.05m, -15.00m, -14.95m, -14.90m, -14.85m, -14.80m, -14.75m, -14.70m,
		-14.65m, -14.60m, -14.55m, -14.50m, -14.45m, -14.40m, -14.35m, -14.30m, -14.25m, -14.20m,
		-14.15m, -14.10m, -14.05m, -14.00m, -13.95m, -13.90m, -13.85m, -13.80m, -13.75m, -13.70m,
		-13.65m, -13.60m, -13.55m, -13.50m, -13.45m, -13.40m, -13.35m, -13.30m, -13.25m, -13.20m,
		-13.15m, -13.10m, -13.05m, -13.00m, -12.95m, -12.90m, -12.85m, -12.80m, -12.75m, -12.70m,
		-12.65m, -12.60m, -12.55m, -12.50m, -12.45m, -12.40m, -12.35m, -12.30m, -12.25m, -12.20m,
		-12.15m, -12.10m, -12.05m, -12.00m, -11.95m, -11.90m, -11.85m, -11.80m, -11.75m, -11.70m,
		-11.65m, -11.60m, -11.55m, -11.50m, -11.45m, -11.40m, -11.35m, -11.30m, -11.25m, -11.20m,
		-11.15m, -11.10m, -11.05m, -11.00m, -10.95m, -10.90m, -10.85m, -10.80m, -10.75m, -10.70m,
		-10.65m, -10.60m, -10.55m, -10.50m, -10.45m, -10.40m, -10.35m, -10.30m, -10.25m, -10.20m,
		-10.15m, -10.10m, -10.05m, -10.00m, -9.95m, -9.90m, -9.85m, -9.80m, -9.75m, -9.70m,
		-9.65m, -9.60m, -9.55m, -9.50m, -9.45m, -9.40m, -9.35m, -9.30m, -9.25m, -9.20m,
		-9.15m, -9.10m, -9.05m, -9.00m, -8.95m, -8.90m, -8.85m, -8.80m, -8.75m, -8.70m,
		-8.65m, -8.60m, -8.55m, -8.50m, -8.45m, -8.40m, -8.35m, -8.30m, -8.25m, -8.20m,
		-8.15m, -8.10m, -8.05m, -8.00m, -7.95m, -7.90m, -7.85m, -7.80m, -7.75m, -7.70m,
		-7.65m, -7.60m, -7.55m, -7.50m, -7.45m, -7.40m, -7.35m, -7.30m, -7.25m, -7.20m,
		-7.15m, -7.10m, -7.05m, -7.00m, -6.95m, -6.90m, -6.85m, -6.80m, -6.75m, -6.70m,
		-6.65m, -6.60m, -6.55m, -6.50m, -6.45m, -6.40m, -6.35m, -6.30m, -6.25m, -6.20m,
		-6.15m, -6.10m, -6.05m, -6.00m, -5.95m, -5.90m, -5.85m, -5.80m, -5.75m, -5.70m,
		-5.65m, -5.60m, -5.55m, -5.50m, -5.45m, -5.40m, -5.35m, -5.30m, -5.25m, -5.20m,
		-5.15m, -5.10m, -5.05m, -5.00m, -4.95m, -4.90m, -4.85m, -4.80m, -4.75m, -4.70m,
		-4.65m, -4.60m, -4.55m, -4.50m, -4.45m, -4.40m, -4.35m, -4.30m, -4.25m, -4.20m,
		-4.15m, -4.10m, -4.05m, -4.00m, -3.95m, -3.90m, -3.85m, -3.80m, -3.75m, -3.70m,
		-3.65m, -3.60m, -3.55m, -3.50m, -3.45m, -3.40m, -3.35m, -3.30m, -3.25m, -3.20m,
		-3.15m, -3.10m, -3.05m, -3.00m, -2.95m, -2.90m, -2.85m, -2.80m, -2.75m, -2.70m,
		-2.65m, -2.60m, -2.55m, -2.50m, -2.45m, -2.40m, -2.35m, -2.30m, -2.25m, -2.20m,
		-2.15m, -2.10m, -2.05m, -2.00m, -1.95m, -1.90m, -1.85m, -1.80m, -1.75m, -1.70m,
		-1.65m, -1.60m, -1.55m, -1.50m, -1.45m, -1.40m, -1.35m, -1.30m, -1.25m, -1.20m,
		-1.15m, -1.10m, -1.05m, -1.00m, -0.95m, -0.90m, -0.85m, -0.80m, -0.75m, -0.70m,
		-0.65m, -0.60m, -0.55m, -0.50m, -0.45m, -0.40m, -0.35m, -0.30m, -0.25m, -0.20m,
		-0.15m, -0.10m, -0.05m, 0.00m, 0.05m, 0.10m, 0.15m, 0.20m, 0.25m, 0.30m,
		0.35m, 0.40m, 0.45m, 0.50m, 0.55m, 0.60m, 0.65m, 0.70m, 0.75m, 0.80m,
		0.85m, 0.90m, 0.95m, 1.00m, 1.05m, 1.10m, 1.15m, 1.20m, 1.25m, 1.30m,
		1.35m, 1.40m, 1.45m, 1.50m, 1.55m, 1.60m, 1.65m, 1.70m, 1.75m, 1.80m,
		1.85m, 1.90m, 1.95m, 2.00m, 2.05m, 2.10m, 2.15m, 2.20m, 2.25m, 2.30m,
		2.35m, 2.40m, 2.45m, 2.50m, 2.55m, 2.60m, 2.65m, 2.70m, 2.75m, 2.80m,
		2.85m, 2.90m, 2.95m, 3.00m, 3.05m, 3.10m, 3.15m, 3.20m, 3.25m, 3.30m,
		3.35m, 3.40m, 3.45m, 3.50m, 3.55m, 3.60m, 3.65m, 3.70m, 3.75m, 3.80m,
		3.85m, 3.90m, 3.95m, 4.00m, 4.05m, 4.10m, 4.15m, 4.20m, 4.25m, 4.30m,
		4.35m, 4.40m, 4.45m, 4.50m, 4.55m, 4.60m, 4.65m, 4.70m, 4.75m, 4.80m,
		4.85m, 4.90m, 4.95m, 5.00m, 5.05m, 5.10m, 5.15m, 5.20m, 5.25m, 5.30m,
		5.35m, 5.40m, 5.45m, 5.50m, 5.55m, 5.60m, 5.65m, 5.70m, 5.75m, 5.80m,
		5.85m, 5.90m, 5.95m, 6.00m, 6.05m, 6.10m, 6.15m, 6.20m, 6.25m, 6.30m,
		6.35m, 6.40m, 6.45m, 6.50m, 6.55m, 6.60m, 6.65m, 6.70m, 6.75m, 6.80m,
		6.85m, 6.90m, 6.95m, 7.00m, 7.05m, 7.10m, 7.15m, 7.20m, 7.25m, 7.30m,
		7.35m, 7.40m, 7.45m, 7.50m, 7.55m, 7.60m, 7.65m, 7.70m, 7.75m, 7.80m,
		7.85m, 7.90m, 7.95m, 8.00m, 8.05m, 8.10m, 8.15m, 8.20m, 8.25m, 8.30m,
		8.35m, 8.40m, 8.45m, 8.50m, 8.55m, 8.60m, 8.65m, 8.70m, 8.75m, 8.80m,
		8.85m, 8.90m, 8.95m, 9.00m, 9.05m, 9.10m, 9.15m, 9.20m, 9.25m, 9.30m,
		9.35m, 9.40m, 9.45m, 9.50m, 9.55m, 9.60m, 9.65m, 9.70m, 9.75m, 9.80m,
		9.85m, 9.90m, 9.95m, 10.00m
	};

	private decimal[] HAMap = new decimal[73]
	{
		10m, 9m, 8m, 7m, 6m, 5m, 4m, 3m, 2m, 1m,
		0m, -1m, -2m, -3m, -4m, -5m, -6m, -7m, -8m, -9m,
		-10m, -11m, -12m, -13m, -14m, -15m, -16m, -17m, -18m, -19m,
		-20m, -21m, -22m, -23m, -24m, -25m, -26m, -27m, -28m, -29m,
		-30m, -31m, -32m, -33m, -34m, -35m, -36m, -37m, -38m, -39m,
		-40m, -41m, -42m, -43m, -44m, -45m, -46m, -47m, -48m, -49m,
		-50m, -51m, -52m, -53m, -54m, -55m, -56m, -57m, -58m, -59m,
		-60m, -61m, -62m
	};

	private string[] PanMap = new string[127]
	{
		"L-100", "L-98", "L-97", "L-95", "L-94", "L-92", "L-90", "L-89", "L-87", "L-86",
		"L-84", "L-83", "L-81", "L-79", "L-78", "L-76", "L-75", "L-73", "L-71", "L-70",
		"L-68", "L-67", "L-65", "L-63", "L-62", "L-60", "L-59", "L-57", "L-56", "L-54",
		"L-52", "L-51", "L-49", "L-48", "L-46", "L-44", "L-43", "L-41", "L-40", "L-38",
		"L-37", "L-35", "L-33", "L-32", "L-30", "L-29", "L-27", "L-25", "L-24", "L-22",
		"L-21", "L-19", "L-17", "L-16", "L-14", "L-13", "L-11", "L-10", "L-8", "L-6",
		"L-5", "L-3", "L-2", "Center", "R-2", "R-3", "R-5", "R-6", "R-8", "R-10",
		"R-11", "R-13", "R-14", "R-16", "R-17", "R-19", "R-21", "R-22", "R-24", "R-25",
		"R-27", "R-29", "R-30", "R-32", "R-33", "R-35", "R-37", "R-38", "R-40", "R-41",
		"R-43", "R-44", "R-46", "R-48", "R-49", "R-51", "R-52", "R-54", "R-56", "R-57",
		"R-59", "R-60", "R-62", "R-63", "R-65", "R-67", "R-68", "R-70", "R-71", "R-73",
		"R-75", "R-76", "R-78", "R-79", "R-81", "R-83", "R-84", "R-86", "R-87", "R-89",
		"R-90", "R-92", "R-94", "R-95", "R-97", "R-98", "R-100"
	};

	private int[] dbToMeterSteps = new int[96]
	{
		126, 125, 123, 122, 121, 120, 119, 117, 115, 114,
		113, 112, 111, 109, 107, 106, 105, 104, 103, 101,
		99, 98, 97, 96, 95, 93, 91, 90, 89, 88,
		87, 85, 83, 82, 81, 80, 79, 77, 75, 74,
		73, 72, 71, 69, 67, 66, 65, 64, 63, 61,
		59, 58, 57, 56, 55, 53, 51, 50, 49, 48,
		47, 45, 43, 42, 41, 40, 39, 37, 35, 34,
		33, 32, 31, 29, 27, 26, 25, 24, 23, 21,
		19, 18, 17, 16, 15, 13, 11, 10, 9, 8,
		7, 5, 3, 2, 1, 0
	};

	private Pen p;

	private Pen penMajor;

	private Pen penMinor;

	private Pen penBlack;

	private Pen penFader;

	private Pen penMeterB;

	private Pen penMeterG;

	private Pen penMeterY;

	private Pen penMeterR;

	private Brush horizBrush;

	private Brush horizBrushG;

	private Brush horizBrushY;

	private Brush horizBrushR;

	private Brush vertBrush;

	private Brush vertBrushG;

	private Brush vertBrushY;

	private Brush vertBrushR;

	private Font fntArial;

	private Font fntArialSM;

	private StringFormat formatCenter;

	private IContainer components;

	public int Channel
	{
		get
		{
			return channel;
		}
		set
		{
			channel = value;
		}
	}

	public decimal Value
	{
		get
		{
			return barValue;
		}
		set
		{
			if (value < (decimal)barMinimum)
			{
				value = barMinimum;
			}
			if (value > (decimal)barMaximum)
			{
				value = barMaximum;
			}
			if (barValue != value)
			{
				barValue = value;
				Invalidate();
			}
		}
	}

	public decimal ValueDB
	{
		get
		{
			int num = FuzzyFind(faderMap, barValue / barRange * (decimal)(dbMap.Length - 1));
			return dbMap[num];
		}
		set
		{
		}
	}

	public int Maximum
	{
		get
		{
			return barMaximum;
		}
		set
		{
			barMaximum = value;
			barRange = barMaximum - barMinimum;
		}
	}

	public int Minimum
	{
		get
		{
			return barMinimum;
		}
		set
		{
			barMinimum = value;
			barRange = barMaximum - barMinimum;
		}
	}

	public decimal TickMark
	{
		get
		{
			return tickMark;
		}
		set
		{
			tickMark = value;
			Invalidate();
		}
	}

	public int BarStyle
	{
		get
		{
			return barStyle;
		}
		set
		{
			if ((value >= 0 && value <= 99) || value == 99)
			{
				barStyle = value;
			}
		}
	}

	public Color BorderColor
	{
		get
		{
			return _borderColor;
		}
		set
		{
			_borderColor = value;
			Invalidate();
		}
	}

	public Color BarColor
	{
		get
		{
			return _barColor;
		}
		set
		{
			_barColor = value;
			horizBrush = new LinearGradientBrush(base.ClientRectangle, _barColor, ControlPaint.Dark(_barColor, 0.1f), 90f);
			horizBrushG = new LinearGradientBrush(base.ClientRectangle, Color.Lime, ControlPaint.Dark(Color.Lime, 0.1f), 90f);
			horizBrushY = new LinearGradientBrush(base.ClientRectangle, Color.Yellow, ControlPaint.Dark(Color.Yellow, 0.1f), 90f);
			horizBrushR = new LinearGradientBrush(base.ClientRectangle, Color.Red, ControlPaint.Dark(Color.Red, 0.1f), 90f);
			vertBrushG = new LinearGradientBrush(base.ClientRectangle, Color.Lime, ControlPaint.Dark(Color.Lime, 0.1f), 0f);
			vertBrushY = new LinearGradientBrush(base.ClientRectangle, Color.Yellow, ControlPaint.Dark(Color.Yellow, 0.1f), 0f);
			vertBrushR = new LinearGradientBrush(base.ClientRectangle, Color.Red, ControlPaint.Dark(Color.Red, 0.1f), 0f);
			Invalidate();
		}
	}

	private int FuzzyFind(decimal[] group, decimal find)
	{
		int num = Array.BinarySearch(group, find);
		if (num >= 0)
		{
			return num;
		}
		if (num < 0)
		{
			num = -num - 1;
		}
		if (num == 0)
		{
			return num;
		}
		if (num >= group.Length)
		{
			return group.Length - 1;
		}
		if (num > group.Length - 1)
		{
			num = group.Length - 1;
		}
		if (num <= 0)
		{
			MessageBox.Show("WARN: Subzero index (" + num + ") in FuzzyFind. Please notify support!");
		}
		if (num >= group.Length)
		{
			MessageBox.Show("WARN: Overlimit index (" + num + ") in FuzzyFind. Please notify support!");
		}
		decimal num2 = group[num] - find;
		decimal num3 = find - group[num - 1];
		if (num2 <= num3)
		{
			return num;
		}
		return num - 1;
	}

	public AirMeter()
	{
		SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, value: true);
		fntArial = new Font("Arial", 9.75f, FontStyle.Bold, GraphicsUnit.Point, 0);
		fntArialSM = new Font("Arial", 8f, FontStyle.Bold, GraphicsUnit.Point, 0);
		formatCenter = new StringFormat();
		formatCenter.Alignment = StringAlignment.Center;
		p = new Pen(Color.Coral, 6f);
		penMajor = new Pen(Color.GhostWhite, 3f);
		penMinor = new Pen(Color.GhostWhite, 1f);
		penBlack = new Pen(Color.Black, 8f);
		penFader = new Pen(Color.Gold, 2f);
		penMeterB = new Pen(Color.Black, 6f);
		penMeterG = new Pen(Color.LimeGreen, 6f);
		penMeterY = new Pen(Color.Gold, 6f);
		penMeterR = new Pen(Color.OrangeRed, 6f);
		horizBrush = new LinearGradientBrush(base.ClientRectangle, _barColor, ControlPaint.Dark(_barColor, 0.1f), 90f);
		horizBrushG = new LinearGradientBrush(base.ClientRectangle, Color.Lime, ControlPaint.Dark(Color.Lime, 0.1f), 90f);
		horizBrushY = new LinearGradientBrush(base.ClientRectangle, Color.Yellow, ControlPaint.Dark(Color.Yellow, 0.1f), 90f);
		horizBrushR = new LinearGradientBrush(base.ClientRectangle, Color.Red, ControlPaint.Dark(Color.Red, 0.1f), 90f);
		vertBrushG = new LinearGradientBrush(base.ClientRectangle, Color.Lime, ControlPaint.Dark(Color.Lime, 0.1f), 0f);
		vertBrushY = new LinearGradientBrush(base.ClientRectangle, Color.Yellow, ControlPaint.Dark(Color.Yellow, 0.1f), 0f);
		vertBrushR = new LinearGradientBrush(base.ClientRectangle, Color.Red, ControlPaint.Dark(Color.Red, 0.1f), 0f);
		InitializeComponent();
	}

	protected override void OnResize(EventArgs e)
	{
		if (base.ClientRectangle.Width != 0 && base.ClientRectangle.Height != 0)
		{
			horizBrush = new LinearGradientBrush(base.ClientRectangle, _barColor, ControlPaint.Dark(_barColor, 0.1f), 90f);
			horizBrushG = new LinearGradientBrush(base.ClientRectangle, Color.Lime, ControlPaint.Dark(Color.Lime, 0.1f), 90f);
			horizBrushY = new LinearGradientBrush(base.ClientRectangle, Color.Yellow, ControlPaint.Dark(Color.Yellow, 0.1f), 90f);
			horizBrushR = new LinearGradientBrush(base.ClientRectangle, Color.Red, ControlPaint.Dark(Color.Red, 0.1f), 90f);
			vertBrushG = new LinearGradientBrush(base.ClientRectangle, Color.Lime, ControlPaint.Dark(Color.Lime, 0.1f), 0f);
			vertBrushY = new LinearGradientBrush(base.ClientRectangle, Color.Yellow, ControlPaint.Dark(Color.Yellow, 0.1f), 0f);
			vertBrushR = new LinearGradientBrush(base.ClientRectangle, Color.Red, ControlPaint.Dark(Color.Red, 0.1f), 0f);
			Invalidate();
		}
	}

	protected override void OnPaintBackground(PaintEventArgs e)
	{
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		if (barRange == 0m)
		{
			return;
		}
		e.Graphics.Clear(BackColor);
		new Pen(Color.LightGray, 1f);
		float num = 0f;
		double num2 = 31.919547688429603;
		double num3 = -0.19192149311560863;
		double num4 = -0.06327109629724784;
		switch (barStyle)
		{
		case 1:
		{
			Pen pen = new Pen(Color.LightGreen, base.Width - 2);
			int num9 = 0;
			double num10 = 0.0;
			double num11 = 0.0;
			if (barValue >= 126m)
			{
				pen.Color = Color.Red;
				num10 = (double)((barValue - (decimal)barMinimum) / barRange);
				num11 = 0.0139465 + -0.0128718 * num10 + 0.954671 * Math.Pow(num10, 2.0);
				num9 = (int)Math.Round((double)base.Height - 1.0 - num11 * ((double)base.Height - 2.0));
				e.Graphics.DrawLine(pen, base.Width / 2, num9, base.Width / 2, base.Height - 1);
				barValue = 125m;
			}
			if (barValue >= 103m)
			{
				pen.Color = Color.Yellow;
				num10 = (double)((barValue - (decimal)barMinimum) / barRange);
				num11 = 0.0139465 + -0.0128718 * num10 + 0.954671 * Math.Pow(num10, 2.0);
				num9 = (int)Math.Round((double)base.Height - 1.0 - num11 * ((double)base.Height - 2.0));
				e.Graphics.DrawLine(pen, base.Width / 2, num9, base.Width / 2, base.Height - 1);
				barValue = 103m;
			}
			pen.Color = Color.LightGreen;
			num10 = (double)((barValue - (decimal)barMinimum) / barRange);
			num11 = 0.0139465 + -0.0128718 * num10 + 0.954671 * Math.Pow(num10, 2.0);
			num9 = (int)Math.Round((double)base.Height - 1.0 - num11 * ((double)base.Height - 2.0));
			e.Graphics.DrawLine(pen, base.Width / 2, num9, base.Width / 2, base.Height - 1);
			for (int j = barMinimum; j <= barMaximum; j++)
			{
				num10 = (double)((decimal)(j - barMinimum) / barRange);
				num11 = 0.0139465 + -0.0128718 * num10 + 0.954671 * Math.Pow(num10, 2.0);
				num9 = (int)Math.Round((double)base.Height - 1.0 - num11 * ((double)base.Height - 2.0));
				e.Graphics.DrawLine(Pens.Black, 0, num9, base.Width - 1, num9);
			}
			ControlPaint.DrawBorder(e.Graphics, base.ClientRectangle, _borderColor, ButtonBorderStyle.Solid);
			break;
		}
		case 2:
		{
			double num5 = (double)(barValue - (decimal)barMinimum);
			double num6 = num5 / (num2 + num3 * num5);
			num6 += num4;
			num = base.Height - (int)Math.Round(num6 / 16.0 * (double)(base.Height - 2));
			e.Graphics.FillRectangle(vertBrushG, 1f, num, base.Width - 2, (float)base.Height - num);
			if (((float)base.Height - num) / (float)base.Height > 0.4705f)
			{
				e.Graphics.FillRectangle(vertBrushY, 1f, num, base.Width - 2, (float)base.Height * 0.5295f - num);
			}
			if (((float)base.Height - num) / (float)base.Height > 0.9411f)
			{
				e.Graphics.FillRectangle(vertBrushR, 1f, num, base.Width - 2, (float)base.Height * 0.0589f - num);
			}
			for (int num7 = 34; num7 > 18; num7--)
			{
				e.Graphics.DrawLine(Pens.DarkGreen, 1f, (float)num7 / 34f * (float)base.Height, base.Width - 2, (float)num7 / 34f * (float)base.Height);
			}
			for (int num8 = 18; num8 > 1; num8--)
			{
				e.Graphics.DrawLine(Pens.DarkGoldenrod, 1f, (float)num8 / 34f * (float)base.Height, base.Width - 2, (float)num8 / 34f * (float)base.Height);
			}
			e.Graphics.DrawLine(Pens.DarkRed, 1f, 1f / 34f * (float)base.Height, base.Width - 2, 1f / 34f * (float)base.Height);
			ControlPaint.DrawBorder(e.Graphics, base.ClientRectangle, _borderColor, ButtonBorderStyle.Solid);
			break;
		}
		case 10:
		{
			double num5 = (double)(barValue - (decimal)barMinimum);
			double num6 = num5 / (num2 + num3 * num5);
			num6 += num4;
			num = (int)Math.Round(num6 / 16.0 * (double)(base.Width - 2));
			e.Graphics.FillRectangle(horizBrushG, 1f, 6f, num, base.Height - 7);
			if (num / (float)base.Width > 0.4706f)
			{
				e.Graphics.FillRectangle(horizBrushY, (float)base.Width * 0.4706f, 6f, num - (float)base.Width * 0.4706f, base.Height - 7);
			}
			if (num / (float)base.Width > 0.9411f)
			{
				e.Graphics.FillRectangle(horizBrushR, (float)base.Width * 0.9411f, 6f, num - (float)base.Width * 0.94f, base.Height - 7);
			}
			for (int k = 1; k < 9; k++)
			{
				e.Graphics.DrawLine(Pens.DarkGreen, (float)k / 17f * (float)base.Width, 6f, (float)k / 17f * (float)base.Width, base.Height - 2);
			}
			for (int l = 9; l < 16; l++)
			{
				e.Graphics.DrawLine(Pens.DarkGoldenrod, (float)l / 17f * (float)base.Width, 6f, (float)l / 17f * (float)base.Width, base.Height - 2);
			}
			e.Graphics.DrawLine(Pens.DarkRed, 0.9411765f * (float)base.Width, 6f, 0.9411765f * (float)base.Width, base.Height - 2);
			if (Math.Round(Math.Abs(tickMark)) >= 0m && Math.Round(Math.Abs(tickMark)) < (decimal)dbToMeterSteps.Count())
			{
				num5 = dbToMeterSteps[(int)Math.Round(Math.Abs(tickMark))];
				num6 = num5 / (num2 + num3 * num5);
				num6 += num4;
				num = (int)Math.Round(num6 / 16.0 * (double)(base.Width - 2));
				e.Graphics.FillPolygon(Brushes.White, new Point[3]
				{
					new Point((int)num - 4, 0),
					new Point((int)num + 4, 0),
					new Point((int)num, 4)
				});
			}
			e.Graphics.DrawRectangle(Pens.Gray, 0, 5, base.Width - 1, base.Height - 6);
			break;
		}
		case 11:
		{
			double num5 = (double)((barValue - (decimal)barMinimum) / barRange);
			num = (int)Math.Round(num5 * (double)(base.Width - 2));
			e.Graphics.FillRectangle(horizBrush, (float)base.Width - num, 6f, num - 1f, base.Height - 7);
			for (int i = 1; i < 17; i++)
			{
				e.Graphics.DrawLine(Pens.SaddleBrown, (float)i / 17f * (float)base.Width, 6f, (float)i / 17f * (float)base.Width, base.Height - 2);
			}
			e.Graphics.DrawRectangle(Pens.Gray, 0, 5, base.Width - 1, base.Height - 6);
			break;
		}
		case 12:
		{
			double num5 = (double)((barValue - (decimal)barMinimum) / barRange);
			num = (int)Math.Round(num5 * (double)(base.Width - 2));
			e.Graphics.FillRectangle(new SolidBrush(ForeColor), 1f, 1f, num - 1f, base.Height - 2);
			ControlPaint.DrawBorder(e.Graphics, base.ClientRectangle, _borderColor, ButtonBorderStyle.Solid);
			break;
		}
		}
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing && components != null)
		{
			components.Dispose();
		}
		base.Dispose(disposing);
	}

	private void InitializeComponent()
	{
		base.SuspendLayout();
		base.AutoScaleDimensions = new System.Drawing.SizeF(6f, 13f);
		base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
		this.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
		base.Name = "AirMeter";
		base.Size = new System.Drawing.Size(90, 88);
		base.ResumeLayout(false);
	}
}
