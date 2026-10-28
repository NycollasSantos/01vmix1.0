using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Text;
using System.IO;
using System.Windows.Forms;
using AirControls;
using AirFader.Properties;

namespace AirFader;

public class frmBounce : Form
{
	private delegate void ChannelChangedCB(Channel.Pr param, int channel, int paramIndex, int value);

	private const int SCREEN_WIDTH = 960;

	private const int SCREEN_HEIGHT = 720;

	private const int SCREEN_GAP = 80;

	private decimal[] faderMapLS9 = new decimal[1024]
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

	private decimal[] faderMap01V96 = new decimal[1024]
	{
		0.000m, 0.356m, 0.712m, 1.068m, 1.424m, 1.780m, 2.136m, 2.493m, 2.849m, 3.205m,
		3.561m, 3.917m, 4.273m, 4.629m, 4.985m, 5.341m, 5.697m, 6.053m, 6.409m, 6.766m,
		7.122m, 7.478m, 7.834m, 8.190m, 8.546m, 8.902m, 9.258m, 9.614m, 9.970m, 10.326m,
		10.682m, 11.039m, 11.395m, 11.751m, 12.107m, 12.463m, 12.819m, 13.175m, 13.531m, 13.887m,
		14.243m, 14.599m, 14.955m, 15.312m, 15.668m, 16.024m, 16.380m, 16.736m, 17.092m, 17.448m,
		17.804m, 18.160m, 18.516m, 18.872m, 19.228m, 19.585m, 19.941m, 20.297m, 20.653m, 21.009m,
		21.365m, 21.721m, 22.077m, 22.433m, 22.789m, 23.145m, 23.501m, 23.858m, 24.214m, 24.570m,
		24.926m, 25.282m, 25.638m, 25.994m, 26.350m, 26.706m, 27.062m, 27.418m, 27.774m, 28.131m,
		28.487m, 28.843m, 29.199m, 29.555m, 29.911m, 30.267m, 30.623m, 30.979m, 31.335m, 31.691m,
		32.047m, 32.404m, 32.760m, 33.116m, 33.472m, 33.828m, 34.184m, 34.540m, 34.896m, 35.252m,
		35.608m, 35.964m, 36.320m, 36.677m, 37.033m, 37.389m, 37.745m, 38.101m, 38.457m, 38.813m,
		39.169m, 39.525m, 39.881m, 40.237m, 40.593m, 40.950m, 41.306m, 41.662m, 42.018m, 42.374m,
		42.730m, 43.086m, 43.442m, 43.798m, 43.944m, 44.090m, 44.236m, 44.382m, 44.528m, 44.674m,
		44.820m, 44.966m, 45.112m, 45.258m, 45.404m, 45.550m, 45.696m, 45.842m, 45.988m, 46.134m,
		46.280m, 46.426m, 46.572m, 46.718m, 46.864m, 47.010m, 47.156m, 47.302m, 47.448m, 47.594m,
		47.740m, 47.886m, 48.032m, 48.178m, 48.324m, 48.470m, 48.616m, 48.762m, 48.908m, 49.054m,
		49.200m, 49.346m, 49.492m, 49.638m, 49.784m, 49.930m, 50.076m, 50.222m, 50.368m, 50.514m,
		50.660m, 50.806m, 50.952m, 51.098m, 52.287m, 53.475m, 54.664m, 55.853m, 57.042m, 58.231m,
		59.420m, 60.608m, 61.797m, 62.986m, 64.175m, 65.364m, 66.552m, 67.741m, 68.930m, 70.119m,
		71.308m, 72.496m, 73.685m, 74.874m, 76.063m, 77.252m, 78.440m, 79.629m, 80.818m, 82.007m,
		83.196m, 84.384m, 85.573m, 86.762m, 87.951m, 89.140m, 90.329m, 91.517m, 92.706m, 93.895m,
		95.084m, 96.273m, 97.461m, 98.650m, 99.839m, 101.028m, 102.217m, 103.405m, 104.594m, 105.783m,
		106.972m, 108.161m, 109.349m, 110.538m, 111.101m, 111.664m, 112.228m, 112.791m, 113.354m, 113.917m,
		114.480m, 115.043m, 115.606m, 116.169m, 116.733m, 117.296m, 117.859m, 118.422m, 118.985m, 119.548m,
		120.111m, 120.674m, 121.237m, 121.801m, 122.364m, 122.927m, 123.490m, 124.053m, 124.616m, 125.179m,
		125.742m, 126.306m, 126.869m, 127.432m, 127.995m, 128.558m, 129.121m, 129.684m, 130.247m, 130.811m,
		131.374m, 131.937m, 132.500m, 133.063m, 133.626m, 134.189m, 134.752m, 135.315m, 135.879m, 136.442m,
		137.005m, 137.568m, 138.131m, 138.694m, 139.257m, 139.820m, 140.384m, 140.947m, 141.510m, 142.073m,
		142.636m, 143.199m, 143.762m, 144.325m, 144.889m, 145.452m, 146.015m, 146.578m, 147.141m, 147.704m,
		148.267m, 148.830m, 149.393m, 149.957m, 150.520m, 151.083m, 151.646m, 152.209m, 152.772m, 153.335m,
		153.898m, 154.462m, 155.025m, 155.588m, 156.151m, 156.714m, 157.277m, 157.840m, 158.403m, 158.966m,
		159.530m, 160.093m, 160.656m, 161.219m, 161.782m, 162.345m, 162.908m, 163.471m, 164.035m, 164.598m,
		165.161m, 165.724m, 166.287m, 166.850m, 168.258m, 169.666m, 171.074m, 172.481m, 173.889m, 175.297m,
		176.705m, 178.113m, 179.520m, 180.928m, 182.336m, 183.744m, 185.152m, 186.559m, 187.967m, 189.375m,
		190.783m, 192.191m, 193.598m, 195.006m, 196.414m, 197.822m, 199.230m, 200.637m, 202.045m, 203.453m,
		204.861m, 206.269m, 207.676m, 209.084m, 210.492m, 211.900m, 213.307m, 214.715m, 216.123m, 217.531m,
		218.939m, 220.346m, 221.754m, 223.162m, 224.570m, 225.978m, 227.385m, 228.793m, 230.201m, 231.609m,
		233.017m, 234.424m, 235.832m, 237.240m, 238.648m, 240.056m, 241.463m, 242.871m, 244.279m, 245.687m,
		247.095m, 248.502m, 249.910m, 251.318m, 252.726m, 254.134m, 255.541m, 256.949m, 258.357m, 259.765m,
		261.173m, 262.580m, 263.988m, 265.396m, 266.804m, 268.212m, 269.619m, 271.027m, 272.435m, 273.843m,
		275.251m, 276.658m, 278.066m, 279.474m, 280.882m, 282.290m, 283.697m, 285.105m, 286.513m, 287.921m,
		289.329m, 290.736m, 292.144m, 293.552m, 294.960m, 296.368m, 297.775m, 299.183m, 300.591m, 301.999m,
		303.407m, 304.814m, 306.222m, 307.630m, 308.756m, 309.882m, 311.009m, 312.135m, 313.261m, 314.387m,
		315.514m, 316.640m, 317.766m, 318.892m, 320.019m, 321.145m, 322.271m, 323.397m, 324.524m, 325.650m,
		326.776m, 327.902m, 329.029m, 330.155m, 331.281m, 332.407m, 333.533m, 334.660m, 335.786m, 336.912m,
		338.038m, 339.165m, 340.291m, 341.417m, 342.543m, 343.670m, 344.796m, 345.922m, 347.048m, 348.175m,
		349.301m, 350.427m, 351.553m, 352.680m, 353.806m, 354.932m, 356.058m, 357.184m, 358.311m, 359.437m,
		360.563m, 361.689m, 362.816m, 363.942m, 365.068m, 366.194m, 367.321m, 368.447m, 369.573m, 370.699m,
		371.826m, 372.952m, 374.078m, 375.204m, 376.331m, 377.457m, 378.583m, 379.709m, 380.835m, 381.962m,
		383.088m, 384.214m, 385.340m, 386.467m, 387.593m, 388.719m, 389.845m, 390.972m, 392.098m, 393.224m,
		394.350m, 395.477m, 396.603m, 397.729m, 398.855m, 399.982m, 401.108m, 402.234m, 403.360m, 404.486m,
		405.613m, 406.739m, 407.865m, 408.991m, 410.118m, 411.244m, 412.370m, 413.496m, 414.623m, 415.749m,
		416.875m, 418.001m, 419.128m, 420.254m, 421.662m, 423.069m, 424.477m, 425.885m, 427.293m, 428.701m,
		430.108m, 431.516m, 432.924m, 434.332m, 435.740m, 437.147m, 438.555m, 439.963m, 441.371m, 442.779m,
		444.186m, 445.594m, 447.002m, 448.410m, 449.818m, 451.225m, 452.633m, 454.041m, 455.449m, 456.857m,
		458.264m, 459.672m, 461.080m, 462.488m, 463.896m, 465.303m, 466.711m, 468.119m, 469.527m, 470.935m,
		472.342m, 473.750m, 475.158m, 476.566m, 477.974m, 479.381m, 480.789m, 482.197m, 483.605m, 485.013m,
		486.420m, 487.828m, 489.236m, 490.644m, 492.052m, 493.459m, 494.867m, 496.275m, 497.683m, 499.091m,
		500.498m, 501.906m, 503.314m, 504.722m, 506.130m, 507.537m, 508.945m, 510.353m, 511.761m, 513.169m,
		514.576m, 515.984m, 517.392m, 518.800m, 520.207m, 521.615m, 523.023m, 524.431m, 525.839m, 527.246m,
		528.654m, 530.062m, 531.470m, 532.878m, 534.285m, 535.693m, 537.101m, 538.509m, 539.917m, 541.324m,
		542.732m, 544.140m, 545.548m, 546.956m, 548.363m, 549.771m, 551.179m, 552.587m, 553.995m, 555.402m,
		556.810m, 558.218m, 559.626m, 561.034m, 562.431m, 563.828m, 565.226m, 566.623m, 568.020m, 569.418m,
		570.815m, 572.213m, 573.610m, 575.007m, 576.405m, 577.802m, 579.199m, 580.597m, 581.994m, 583.392m,
		584.789m, 586.186m, 587.584m, 588.981m, 590.378m, 591.776m, 593.173m, 594.571m, 595.968m, 597.365m,
		598.763m, 600.160m, 601.557m, 602.955m, 604.352m, 605.749m, 607.147m, 608.544m, 609.942m, 611.339m,
		612.736m, 614.134m, 615.531m, 616.928m, 618.326m, 619.723m, 621.121m, 622.518m, 623.915m, 625.313m,
		626.710m, 628.107m, 629.505m, 630.902m, 632.300m, 633.697m, 635.094m, 636.492m, 637.889m, 639.286m,
		640.684m, 642.081m, 643.478m, 644.876m, 646.273m, 647.671m, 649.068m, 650.465m, 651.863m, 653.260m,
		654.657m, 656.055m, 657.452m, 658.850m, 660.247m, 661.644m, 663.042m, 664.439m, 665.836m, 667.234m,
		668.631m, 670.029m, 671.426m, 672.823m, 674.221m, 675.618m, 677.015m, 678.413m, 679.810m, 681.207m,
		682.605m, 684.002m, 685.400m, 686.797m, 688.194m, 689.592m, 690.989m, 692.386m, 693.784m, 695.181m,
		696.579m, 697.976m, 699.373m, 700.771m, 701.928m, 703.086m, 704.243m, 705.401m, 706.558m, 707.716m,
		708.873m, 710.031m, 711.188m, 712.346m, 713.503m, 714.661m, 715.818m, 716.976m, 718.133m, 719.291m,
		720.449m, 721.606m, 722.764m, 723.921m, 725.079m, 726.236m, 727.394m, 728.551m, 729.709m, 730.866m,
		732.024m, 733.181m, 734.339m, 735.496m, 736.654m, 737.811m, 738.969m, 740.126m, 741.284m, 742.441m,
		743.599m, 744.757m, 745.914m, 747.072m, 748.229m, 749.387m, 750.544m, 751.702m, 752.859m, 754.017m,
		755.174m, 756.332m, 757.489m, 758.647m, 759.804m, 760.962m, 762.119m, 763.277m, 764.434m, 765.592m,
		766.749m, 767.907m, 769.064m, 770.222m, 771.380m, 772.537m, 773.695m, 774.852m, 776.010m, 777.167m,
		778.325m, 779.482m, 780.640m, 781.797m, 782.955m, 784.112m, 785.270m, 786.427m, 787.585m, 788.742m,
		789.900m, 791.057m, 792.215m, 793.372m, 794.530m, 795.688m, 796.845m, 798.003m, 799.160m, 800.318m,
		801.475m, 802.633m, 803.790m, 804.948m, 806.105m, 807.263m, 808.420m, 809.578m, 810.735m, 811.893m,
		813.050m, 814.208m, 815.365m, 816.523m, 817.660m, 818.796m, 819.933m, 821.070m, 822.206m, 823.343m,
		824.480m, 825.616m, 826.753m, 827.890m, 829.026m, 830.163m, 831.300m, 832.436m, 833.573m, 834.710m,
		835.846m, 836.983m, 838.120m, 839.256m, 840.393m, 841.530m, 842.666m, 843.803m, 844.940m, 846.076m,
		847.213m, 848.350m, 849.486m, 850.623m, 851.760m, 852.896m, 854.033m, 855.170m, 856.306m, 857.443m,
		858.580m, 859.716m, 860.853m, 861.990m, 863.126m, 864.263m, 865.400m, 866.536m, 867.673m, 868.810m,
		869.946m, 871.083m, 872.220m, 873.356m, 874.493m, 875.630m, 876.766m, 877.903m, 879.040m, 880.176m,
		881.313m, 882.450m, 883.586m, 884.723m, 885.860m, 886.996m, 888.133m, 889.270m, 890.406m, 891.543m,
		892.680m, 893.816m, 894.953m, 896.090m, 897.226m, 898.363m, 899.500m, 900.636m, 901.773m, 902.910m,
		904.046m, 905.183m, 906.320m, 907.456m, 908.593m, 909.730m, 910.866m, 912.003m, 913.140m, 914.276m,
		915.413m, 916.550m, 917.686m, 918.823m, 919.960m, 921.096m, 922.233m, 923.370m, 924.506m, 925.643m,
		926.780m, 927.916m, 929.053m, 930.190m, 931.118m, 932.046m, 932.974m, 933.902m, 934.830m, 935.758m,
		936.686m, 937.614m, 938.543m, 939.471m, 940.399m, 941.327m, 942.255m, 943.183m, 944.111m, 945.039m,
		945.967m, 946.895m, 947.824m, 948.752m, 949.680m, 950.608m, 951.536m, 952.464m, 953.392m, 954.320m,
		955.248m, 956.177m, 957.105m, 958.033m, 958.961m, 959.889m, 960.817m, 961.745m, 962.673m, 963.601m,
		964.529m, 965.458m, 966.386m, 967.314m, 968.242m, 969.170m, 970.098m, 971.026m, 971.954m, 972.882m,
		973.810m, 974.739m, 975.667m, 976.595m, 977.523m, 978.451m, 979.379m, 980.307m, 981.235m, 982.163m,
		983.092m, 984.020m, 984.948m, 985.876m, 986.804m, 987.732m, 988.660m, 989.588m, 990.516m, 991.444m,
		992.373m, 993.301m, 994.229m, 995.157m, 996.085m, 997.013m, 997.941m, 998.869m, 999.797m, 1000.726m,
		1001.654m, 1002.582m, 1003.510m, 1004.438m, 1005.366m, 1006.294m, 1007.222m, 1008.150m, 1009.078m, 1010.007m,
		1010.935m, 1011.863m, 1012.791m, 1013.719m, 1014.647m, 1015.575m, 1016.503m, 1017.431m, 1018.359m, 1019.288m,
		1020.216m, 1021.144m, 1022.072m, 1023.000m
	};

	private decimal[] faderMap;

	private byte[] meterData;

	private PictureBox picBoxPlayer;

	private PictureBox picBoxAI;

	private Timer gameTime;

	private Size sizePlayer = new Size(40, 100);

	private Size sizeAI = new Size(40, 100);

	private Size sizeBall = new Size(6, 6);

	private decimal ballSpeedX = 0.4m;

	private decimal ballSpeedY = 0.2m;

	private int gameTimeInterval = 10;

	private int player1ch = -1;

	private int player2ch = -1;

	private int player1Score;

	private int player2Score;

	private Point[] plots;

	private Point[] eqPoints;

	private Point ptBall;

	private decimal pongFreq = 60m;

	private decimal pongGain;

	private IContainer components;

	private AirLabel lblPlayer1Ch;

	private AirLabel lblPlayer2Ch;

	private AirLabel lblPlayer1Score;

	private AirLabel lblPlayer2Score;

	private AirLabel lblPlayer1;

	private AirLabel lblPlayer2;

	private AirLabel lblHelp;

	private AirPanel peqPong;

	private AirButton btnExit;

	private AirButton btnAgain;

	public frmBounce()
	{
		InitializeComponent();
		picBoxPlayer = new PictureBox();
		picBoxAI = new PictureBox();
		gameTime = new Timer();
		gameTime.Enabled = false;
		gameTime.Interval = gameTimeInterval;
		gameTime.Tick += gameTime_Tick;
		base.ClientSize = new Size(960, 720);
		base.StartPosition = FormStartPosition.CenterScreen;
		picBoxPlayer.Size = sizePlayer;
		picBoxPlayer.Location = new Point(picBoxPlayer.Width, base.ClientSize.Height / 2 - picBoxPlayer.Height / 2);
		picBoxPlayer.BackColor = Color.Black;
		picBoxPlayer.Image = Resources.thumb_trans;
		picBoxPlayer.Image.RotateFlip(RotateFlipType.RotateNoneFlipX);
		picBoxPlayer.SizeMode = PictureBoxSizeMode.StretchImage;
		base.Controls.Add(picBoxPlayer);
		picBoxAI.Size = sizeAI;
		picBoxAI.Location = new Point(base.ClientSize.Width - (picBoxAI.Width + picBoxAI.Width / 2), base.ClientSize.Height / 2 - picBoxPlayer.Height / 2);
		picBoxAI.BackColor = Color.Black;
		picBoxAI.Image = Resources.thumb_trans;
		picBoxAI.SizeMode = PictureBoxSizeMode.StretchImage;
		base.Controls.Add(picBoxAI);
		lblPlayer2.Left = base.ClientRectangle.Width - lblPlayer2.Width - 10;
		lblPlayer2Ch.Left = base.ClientRectangle.Width - lblPlayer2Ch.Width - 10;
		lblPlayer2Score.Left = base.ClientRectangle.Width - lblPlayer2Score.Width - 10;
		lblHelp.Width = lblPlayer2.Left - lblHelp.Left;
		btnExit.Left = base.ClientRectangle.Width / 2 + 10;
		btnAgain.Left = base.ClientRectangle.Width / 2 - 10 - btnAgain.Width;
		lblPlayer1.Hide();
		lblPlayer2.Hide();
		ConsoleTypes type = Console.type;
		if (type == ConsoleTypes.O1V96V2)
		{
			faderMap = faderMap01V96;
		}
		else
		{
			faderMap = faderMapLS9;
		}
		meterData = File.ReadAllBytes("MeterData.dat");
		BackColor = Settings.Default.c_background;
		btnExit.BackColor = Settings.Default.c_button_inactive;
		btnExit.ForeColor = Settings.Default.c_button_inactive_fore;
		btnExit.ParentColor = BackColor;
		btnAgain.BackColor = Settings.Default.c_button_active;
		btnAgain.ForeColor = Settings.Default.c_button_active_fore;
		btnAgain.ParentColor = BackColor;
		peqPong.SetBounds(0, 80, 960, 640);
		peqPong.SendToBack();
		plots = new Point[123];
		eqPoints = new Point[4];
		ptBall = default(Point);
		plots[plots.Length - 1].X = 0;
		plots[plots.Length - 1].Y = peqPong.Height / 2;
		plots[plots.Length - 2].X = peqPong.Width;
		plots[plots.Length - 2].Y = peqPong.Height / 2;
		for (int i = 0; i <= 120; i++)
		{
			plots[i].Y = peqPong.Height / 2;
		}
		peqPong.SetPoints(plots, eqPoints, 0, 0);
		btnAgain.Hide();
		MixEngine.ChannelChanged += MixEngine_ChannelChanged;
	}

	protected override void OnClosing(CancelEventArgs e)
	{
		MixEngine.ChannelChanged -= MixEngine_ChannelChanged;
		gameTime.Enabled = false;
		gameTime.Dispose();
		base.OnClosing(e);
	}

	private void MixEngine_ChannelChanged(Channel.Pr param, int channel, int paramIndex, int value)
	{
		if (lblPlayer1Ch.InvokeRequired)
		{
			ChannelChangedCB method = MixEngine_ChannelChanged;
			BeginInvoke(method, param, channel, paramIndex, value);
			return;
		}
		try
		{
			if (MixEngine.chData[channel].channelType != ChannelTypes.STIN || channel % 2 != 1)
			{
				if (param == Channel.Pr.Fader && player1ch == -1)
				{
					player1ch = channel;
					lblPlayer1Ch.Text = MixEngine.chData[player1ch].hardname;
					lblHelp.Text = "Move a DIFFERENT fader to select Player 2's controller.";
				}
				else if (param == Channel.Pr.Fader && player2ch == -1 && channel != player1ch)
				{
					player2ch = channel;
					lblPlayer2Ch.Text = MixEngine.chData[player2ch].hardname;
					gameTime.Enabled = true;
					lblHelp.Text = "Press ON to Pause";
				}
				else if (param == Channel.Pr.On && (channel == player1ch || channel == player2ch))
				{
					gameTime.Enabled = !gameTime.Enabled;
				}
			}
		}
		catch (Exception ex)
		{
			Loc.Msg(ex.ToString());
		}
	}

	private void gameTime_Tick(object sender, EventArgs e)
	{
		pongFreq += ballSpeedX;
		if (pongFreq > 124m)
		{
			pongFreq = 124m;
		}
		if (pongFreq < 4m)
		{
			pongFreq = 4m;
		}
		pongGain += ballSpeedY;
		if (pongGain > 18m)
		{
			pongGain = 18m;
		}
		if (pongGain < -18m)
		{
			pongGain = -18m;
		}
		for (int i = 4; i <= 124; i++)
		{
			plots[i - 4].X = (int)Math.Round((decimal)(peqPong.Width - 1) / 120m * (decimal)(i - 4));
			plots[i - 4].Y = (int)Math.Round((calcGainAt(pongGain, 15m, pongFreq, i) + 20m) / 40m * (decimal)peqPong.Height);
		}
		eqPoints[0].X = (int)Math.Round((decimal)(peqPong.Width - 1) / 120m * (pongFreq - 4m));
		eqPoints[0].Y = (int)Math.Round((calcGainAt(pongGain, 15m, pongFreq, (int)Math.Round(pongFreq)) + 20m) / 40m * (decimal)peqPong.Height);
		ptBall.X = eqPoints[0].X + peqPong.Left;
		ptBall.Y = eqPoints[0].Y + peqPong.Top;
		peqPong.SetPoints(plots, eqPoints, 0, 0);
		gameAreaCollisions();
		padlleCollision();
		playerMovement();
	}

	private void gameAreaCollisions()
	{
		if (pongGain >= 18m || pongGain <= -18m)
		{
			ballSpeedY = -ballSpeedY;
		}
		else if (pongFreq >= 122m)
		{
			player1Score++;
			lblPlayer1Score.Text = "+" + player1Score + "dB";
			resetBall();
		}
		else if (pongFreq <= 6m)
		{
			player2Score++;
			lblPlayer2Score.Text = "+" + player2Score + "dB";
			resetBall();
		}
		if (player1Score >= 10 || player2Score >= 10)
		{
			lblHelp.Text = "Game Over!";
			btnAgain.Show();
			gameTime.Enabled = false;
			resetBall();
		}
	}

	private void resetBall()
	{
		pongGain = 0m;
		pongFreq = 60m;
		ballSpeedX *= 1.05m;
		ballSpeedY *= 1.05m;
	}

	private void playerMovement()
	{
		if (player1ch >= 0)
		{
			int num = picBoxPlayer.Width;
			decimal num2 = faderMap[MixEngine.chData[player1ch].getParam(Channel.Pr.Fader)];
			int num3 = (int)Math.Round((1023m - num2) / 1023.0m * (decimal)(640 - picBoxPlayer.Height) + 80m);
			picBoxPlayer.Location = new Point(num, num3);
		}
		if (player2ch >= 0)
		{
			int num4 = base.ClientSize.Width - (picBoxAI.Width + picBoxAI.Width / 2);
			decimal num5 = faderMap[MixEngine.chData[player2ch].getParam(Channel.Pr.Fader)];
			int num6 = (int)Math.Round((1023m - num5) / 1023.0m * (decimal)(640 - picBoxPlayer.Height) + 80m);
			picBoxAI.Location = new Point(num4, num6);
		}
	}

	private void aiMovement()
	{
		_ = ballSpeedX > 0m;
	}

	private void padlleCollision()
	{
		if (picBoxAI.Bounds.Contains(ptBall) && ballSpeedX > 0m)
		{
			ballSpeedX = -ballSpeedX;
		}
		if (picBoxPlayer.Bounds.Contains(ptBall) && ballSpeedX < 0m)
		{
			ballSpeedX = -ballSpeedX;
		}
	}

	private void frmPong_Load(object sender, EventArgs e)
	{
		Loc.Apply(this);
	}

	private decimal calcGainAt(decimal Gain, decimal Q, decimal Freq, int SampleFreq)
	{
		int num = (int)Q;
		int num2 = (int)Freq;
		if (num < 0)
		{
			num = 0;
		}
		switch (num)
		{
		case 41:
		{
			if (Gain == 0m)
			{
				return 0.0m;
			}
			int num15 = (int)Math.Abs(Gain) / 6;
			if (num15 > 3)
			{
				num15 = 3;
			}
			int num16 = ((num15 >= 3) ? 3 : (num15 + 1));
			decimal num17 = 0.0m;
			decimal num18 = 0.0m;
			if (SampleFreq - num2 + 68 <= 0)
			{
				return Gain;
			}
			if (SampleFreq - num2 + 68 >= 124)
			{
				return 0.0m;
			}
			if (num15 > 0)
			{
				num17 = (decimal)meterData[(num15 - 1) * 124 + (SampleFreq - num2 + 68) + 15251] / 10.0m;
			}
			if (num16 > 0)
			{
				num18 = (decimal)meterData[(num16 - 1) * 124 + (SampleFreq - num2 + 68) + 15251] / 10.0m;
			}
			return (decimal)Math.Sign(Gain) * ((num18 - num17) * (Math.Abs(Gain) - (decimal)num15 * 6.0m) / 6.0m + num17);
		}
		case 42:
		{
			if (Gain == 0m)
			{
				return 0.0m;
			}
			int num7 = (int)Math.Abs(Gain) / 6;
			if (num7 > 3)
			{
				num7 = 3;
			}
			int num8 = ((num7 >= 3) ? 3 : (num7 + 1));
			decimal num9 = 0.0m;
			decimal num10 = 0.0m;
			if (num2 - SampleFreq + 68 <= 0)
			{
				return Gain;
			}
			if (num2 - SampleFreq + 68 >= 124)
			{
				return 0.0m;
			}
			if (num7 > 0)
			{
				num9 = (decimal)meterData[(num7 - 1) * 124 + (num2 - SampleFreq + 68) + 15251] / 10.0m;
			}
			if (num8 > 0)
			{
				num10 = (decimal)meterData[(num8 - 1) * 124 + (num2 - SampleFreq + 68) + 15251] / 10.0m;
			}
			return (decimal)Math.Sign(Gain) * ((num10 - num9) * (Math.Abs(Gain) - (decimal)num7 * 6.0m) / 6.0m + num9);
		}
		case 43:
		{
			if (num2 - SampleFreq + 68 <= 0)
			{
				return -60.0m;
			}
			if (num2 - SampleFreq + 68 >= 124)
			{
				return 0.0m;
			}
			decimal num11 = meterData[(num2 - SampleFreq + 68) * 2 + 15624];
			decimal num12 = meterData[(num2 - SampleFreq + 68) * 2 + 15624 + 1];
			return -(num11 * 256m + num12) / 10.0m;
		}
		case 44:
		{
			if (SampleFreq - num2 + 68 <= 0)
			{
				return -60.0m;
			}
			if (SampleFreq - num2 + 68 >= 124)
			{
				return 0.0m;
			}
			decimal num13 = meterData[(SampleFreq - num2 + 68) * 2 + 15624];
			decimal num14 = meterData[(SampleFreq - num2 + 68) * 2 + 15624 + 1];
			return -(num13 * 256m + num14) / 10.0m;
		}
		default:
		{
			if (Gain == 0m)
			{
				return 0.0m;
			}
			if (Math.Abs(Gain) > 18m)
			{
				Gain = Math.Sign(Gain) * 18;
			}
			if (Gain % 6m == 0m)
			{
				return (decimal)Math.Sign(Gain) * (decimal)meterData[((int)Math.Abs(Gain) / 6 - 1) * 5084 + num * 124 + Math.Abs(num2 - SampleFreq)] / 10.0m;
			}
			int num3 = (int)Math.Abs(Gain) / 6;
			if (num3 > 3)
			{
				num3 = 3;
			}
			int num4 = ((num3 >= 3) ? 3 : (num3 + 1));
			decimal num5 = 0.0m;
			decimal num6 = 0.0m;
			if (num3 > 0)
			{
				num5 = (decimal)meterData[(num3 - 1) * 5084 + num * 124 + Math.Abs(num2 - SampleFreq)] / 10.0m;
			}
			if (num4 > 0)
			{
				num6 = (decimal)meterData[(num4 - 1) * 5084 + num * 124 + Math.Abs(num2 - SampleFreq)] / 10.0m;
			}
			return (decimal)Math.Sign(Gain) * ((num6 - num5) * (Math.Abs(Gain) - (decimal)num3 * 6.0m) / 6.0m + num5);
		}
		}
	}

	private void btnExit_Click(object sender, EventArgs e)
	{
		Close();
	}

	private void btnAgain_Click(object sender, EventArgs e)
	{
		lblHelp.Text = "Press ON to Pause";
		btnAgain.Hide();
		gameTime.Enabled = true;
		ballSpeedX = 0.3m;
		ballSpeedY = 0.2m;
		player1Score = 0;
		player2Score = 0;
		lblPlayer1Score.Text = "+" + player1Score + "dB";
		lblPlayer2Score.Text = "+" + player2Score + "dB";
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
		System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AirFader.frmBounce));
		this.lblPlayer1Ch = new AirControls.AirLabel();
		this.lblPlayer2Ch = new AirControls.AirLabel();
		this.lblPlayer1Score = new AirControls.AirLabel();
		this.lblPlayer2Score = new AirControls.AirLabel();
		this.lblPlayer1 = new AirControls.AirLabel();
		this.lblPlayer2 = new AirControls.AirLabel();
		this.lblHelp = new AirControls.AirLabel();
		this.peqPong = new AirControls.AirPanel();
		this.btnExit = new AirControls.AirButton();
		this.btnAgain = new AirControls.AirButton();
		base.SuspendLayout();
		this.lblPlayer1Ch.Font = new System.Drawing.Font("Arial", 15.75f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.lblPlayer1Ch.ForeColor = System.Drawing.Color.White;
		this.lblPlayer1Ch.Location = new System.Drawing.Point(12, 33);
		this.lblPlayer1Ch.Name = "lblPlayer1Ch";
		this.lblPlayer1Ch.Size = new System.Drawing.Size(132, 24);
		this.lblPlayer1Ch.TabIndex = 2;
		this.lblPlayer1Ch.Text = "???";
		this.lblPlayer1Ch.TextAlign = System.Drawing.ContentAlignment.TopCenter;
		this.lblPlayer1Ch.TextRenderingHint = System.Drawing.Text.TextRenderingHint.SystemDefault;
		this.lblPlayer2Ch.Font = new System.Drawing.Font("Arial", 15.75f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.lblPlayer2Ch.ForeColor = System.Drawing.Color.White;
		this.lblPlayer2Ch.Location = new System.Drawing.Point(593, 33);
		this.lblPlayer2Ch.Name = "lblPlayer2Ch";
		this.lblPlayer2Ch.Size = new System.Drawing.Size(132, 24);
		this.lblPlayer2Ch.TabIndex = 3;
		this.lblPlayer2Ch.Text = "???";
		this.lblPlayer2Ch.TextAlign = System.Drawing.ContentAlignment.TopCenter;
		this.lblPlayer2Ch.TextRenderingHint = System.Drawing.Text.TextRenderingHint.SystemDefault;
		this.lblPlayer1Score.Font = new System.Drawing.Font("Arial", 15.75f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.lblPlayer1Score.ForeColor = System.Drawing.Color.White;
		this.lblPlayer1Score.Location = new System.Drawing.Point(12, 56);
		this.lblPlayer1Score.Name = "lblPlayer1Score";
		this.lblPlayer1Score.Size = new System.Drawing.Size(132, 24);
		this.lblPlayer1Score.TabIndex = 4;
		this.lblPlayer1Score.Text = "+0dB";
		this.lblPlayer1Score.TextAlign = System.Drawing.ContentAlignment.TopCenter;
		this.lblPlayer1Score.TextRenderingHint = System.Drawing.Text.TextRenderingHint.SystemDefault;
		this.lblPlayer2Score.Font = new System.Drawing.Font("Arial", 15.75f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.lblPlayer2Score.ForeColor = System.Drawing.Color.White;
		this.lblPlayer2Score.Location = new System.Drawing.Point(593, 56);
		this.lblPlayer2Score.Name = "lblPlayer2Score";
		this.lblPlayer2Score.Size = new System.Drawing.Size(132, 24);
		this.lblPlayer2Score.TabIndex = 5;
		this.lblPlayer2Score.Text = "+0dB";
		this.lblPlayer2Score.TextAlign = System.Drawing.ContentAlignment.TopCenter;
		this.lblPlayer2Score.TextRenderingHint = System.Drawing.Text.TextRenderingHint.SystemDefault;
		this.lblPlayer1.Font = new System.Drawing.Font("Arial", 15.75f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.lblPlayer1.ForeColor = System.Drawing.Color.Silver;
		this.lblPlayer1.Location = new System.Drawing.Point(12, 9);
		this.lblPlayer1.Name = "lblPlayer1";
		this.lblPlayer1.Size = new System.Drawing.Size(132, 24);
		this.lblPlayer1.TabIndex = 6;
		this.lblPlayer1.Text = "Player 1";
		this.lblPlayer1.TextAlign = System.Drawing.ContentAlignment.TopCenter;
		this.lblPlayer1.TextRenderingHint = System.Drawing.Text.TextRenderingHint.SystemDefault;
		this.lblPlayer2.Font = new System.Drawing.Font("Arial", 15.75f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.lblPlayer2.ForeColor = System.Drawing.Color.Silver;
		this.lblPlayer2.Location = new System.Drawing.Point(593, 9);
		this.lblPlayer2.Name = "lblPlayer2";
		this.lblPlayer2.Size = new System.Drawing.Size(132, 24);
		this.lblPlayer2.TabIndex = 7;
		this.lblPlayer2.Text = "Player 2";
		this.lblPlayer2.TextAlign = System.Drawing.ContentAlignment.TopCenter;
		this.lblPlayer2.TextRenderingHint = System.Drawing.Text.TextRenderingHint.SystemDefault;
		this.lblHelp.Font = new System.Drawing.Font("Arial", 12f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.lblHelp.ForeColor = System.Drawing.Color.Yellow;
		this.lblHelp.Location = new System.Drawing.Point(150, 13);
		this.lblHelp.Name = "lblHelp";
		this.lblHelp.Size = new System.Drawing.Size(456, 24);
		this.lblHelp.TabIndex = 8;
		this.lblHelp.Text = "Move any fader to select Player 1's controller.";
		this.lblHelp.TextAlign = System.Drawing.ContentAlignment.TopCenter;
		this.lblHelp.TextRenderingHint = System.Drawing.Text.TextRenderingHint.SystemDefault;
		this.peqPong.BorderColor = System.Drawing.Color.Black;
		this.peqPong.BorderSize = 1f;
		this.peqPong.EQColor = System.Drawing.Color.Lime;
		this.peqPong.Location = new System.Drawing.Point(16, 83);
		this.peqPong.Name = "peqPong";
		this.peqPong.PanelType = 1;
		this.peqPong.Size = new System.Drawing.Size(916, 569);
		this.peqPong.TabIndex = 9;
		this.btnExit.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
		this.btnExit.BackText = "";
		this.btnExit.BorderColor = System.Drawing.Color.Black;
		this.btnExit.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.btnExit.LEDColor = System.Drawing.Color.Gray;
		this.btnExit.Location = new System.Drawing.Point(317, 33);
		this.btnExit.Name = "btnExit";
		this.btnExit.ParentColor = System.Drawing.Color.Gray;
		this.btnExit.Size = new System.Drawing.Size(104, 44);
		this.btnExit.TabIndex = 10;
		this.btnExit.Text = "Exit";
		this.btnExit.UseVisualStyleBackColor = true;
		this.btnExit.Click += new System.EventHandler(btnExit_Click);
		this.btnAgain.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
		this.btnAgain.BackText = "";
		this.btnAgain.BorderColor = System.Drawing.Color.Black;
		this.btnAgain.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.btnAgain.LEDColor = System.Drawing.Color.Gray;
		this.btnAgain.Location = new System.Drawing.Point(207, 33);
		this.btnAgain.Name = "btnAgain";
		this.btnAgain.ParentColor = System.Drawing.Color.Gray;
		this.btnAgain.Size = new System.Drawing.Size(104, 44);
		this.btnAgain.TabIndex = 11;
		this.btnAgain.Text = "Again!";
		this.btnAgain.UseVisualStyleBackColor = true;
		this.btnAgain.Click += new System.EventHandler(btnAgain_Click);
		base.AutoScaleDimensions = new System.Drawing.SizeF(6f, 13f);
		base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
		this.BackColor = System.Drawing.Color.FromArgb(64, 64, 64);
		base.ClientSize = new System.Drawing.Size(944, 682);
		base.Controls.Add(this.btnAgain);
		base.Controls.Add(this.btnExit);
		base.Controls.Add(this.peqPong);
		base.Controls.Add(this.lblPlayer2);
		base.Controls.Add(this.lblPlayer1);
		base.Controls.Add(this.lblPlayer2Score);
		base.Controls.Add(this.lblPlayer1Score);
		base.Controls.Add(this.lblPlayer2Ch);
		base.Controls.Add(this.lblPlayer1Ch);
		base.Controls.Add(this.lblHelp);
		base.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
		base.Icon = System.Drawing.Icon.ExtractAssociatedIcon(System.Windows.Forms.Application.ExecutablePath);
		base.Name = "frmBounce";
		this.Text = "Bounce";
		base.Load += new System.EventHandler(frmPong_Load);
		base.ResumeLayout(false);
	}
}
