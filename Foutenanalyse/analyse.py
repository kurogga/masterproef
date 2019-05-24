# Fouten met PSNR en SSIM van 2 beelden berekenen en plotten
# originele beelden worden opgeslagen in original-folder
# changed-folder bevat gereconstrueerde beelden
# beelden worden genummerd vanaf 1

import numpy as np
import matplotlib.pyplot as plt
import cv2
import math
import os
import sys
from skimage import img_as_float
from skimage.measure import compare_ssim as ssim

def get_mse(x, y):
#    return mse(x,y)
    return np.linalg.norm(x - y)

def get_ssim(x, y):
    luma_x = np.empty((len(x),len(x[0])))
    luma_y = np.empty((len(y),len(y[0])))
    for i in range(0,len(x)):
        for j in range(0,len(x[i])):
#            luma_x[i][j] = 0.2126*x[i][j][2] + 0.7152*x[i][j][1] + 0.0722*x[i][j][0]
#            luma_y[i][j] = 0.2126*y[i][j][2] + 0.7152*y[i][j][1] + 0.0722*y[i][j][0]
            luma_x[i][j] = 0.2989*x[i][j][2] + 0.5870*x[i][j][1] + 0.1140*x[i][j][0]
            luma_y[i][j] = 0.2989*y[i][j][2] + 0.5870*y[i][j][1] + 0.1140*y[i][j][0]
    ssim_ = ssim(luma_x, luma_y, data_range=luma_y.max() - luma_y.min())
#    print(ssim_)
    return ssim_

def get_psnr(x, y):
#    return psnr(x, y, data_range=y.max() - y.min())
#    print(x.size)
    _mse = get_mse(x, y)
    if _mse == 0:
        return 100
    PIXEL_MAX = 255.0
    return 20 * math.log10(PIXEL_MAX / math.sqrt(_mse))

def compare_images(x, y, ssim_array, psnr_array):
    original_file = cv2.imread(x)
    img = img_as_float(original_file)
#    img = cv2.cvtColor(original_file, cv2.COLOR_RGB2HSV)
    changed_file = cv2.imread(y)
    img_noise = img_as_float(changed_file)
#    img_noise = cv2.cvtColor(changed_file, cv2.COLOR_RGB2HSV)
    ssim_noise = get_ssim(img, img_noise)
    psnr_noise = get_psnr(img, img_noise)
    ssim_array.append(ssim_noise)
    psnr_array.append(psnr_noise)

def plot_graph(n, ssim_arrays, psnr_arrays, labels):
    fig, axes = plt.subplots(
        nrows=1, ncols=2, figsize=(10, 4), sharex=True, sharey=False
    )
    ax = axes.ravel()
    x_max = len(ssim_arrays[0])
    for x in range(0,len(ssim_arrays)):
        ax[0].plot(ssim_arrays[x], label=labels[x])
        print(labels[x]+"-SSIM: "+str(np.mean(ssim_arrays[x])))
    ax[0].set_ylabel("SSIM [%]")
    ax[0].set_xlabel("frame number")
    ax[0].set_title("Structural similarity")
    ax[0].set_ylim([0.0, 1.0])
    ax[0].set_xlim([0, x_max])
    ax[0].legend(loc='best')
    ax[0].grid(True)

    for x in range(0,len(psnr_arrays)):
        ax[1].plot(psnr_arrays[x], label=labels[x])
        print(labels[x]+"-PSNR: "+str(np.mean(psnr_arrays[x])))
    ax[1].set_ylabel("PSNR [dB]")
    ax[1].set_xlabel("frame number")
    ax[1].set_title("Peak signal-to-noise ratio")
    ax[1].set_ylim([10, 60])
    ax[1].legend(loc='best')
    ax[1].grid(True)
    
    plt.tight_layout()
    plt.show()
    
path_original = "original/"
path_changed = "changed/"

# for comparison of 2 or more folders of images 
if len(sys.argv) > 2:
    path_original = sys.argv[1] + "/"
    files = next(os.walk(path_original))[2]
    
    if len(files) == 0:
        sys.exit("Error: not enough files!")
    
    ssim_arrays = []
    psnr_arrays = []    
    labels = []
    for x in range(2,len(sys.argv)):
        templabel = sys.argv[x].split('_',1)
        labels.append(templabel[1])
        path_changed = sys.argv[x] + "/"
        files2 = next(os.walk(path_changed))[2]
        amount = min(len(files),len(files2))
        ssim_array = []
        psnr_array = []
        ssim_arrays.append(ssim_array)
        psnr_arrays.append(psnr_array)
        
        for y in range(0, amount):
            compare_images(path_original + files[y], path_changed + files2[y],ssim_arrays[x-2],psnr_arrays[x-2])
            
    plot_graph(x-2,ssim_arrays,psnr_arrays,labels)
    
else:
    sys.exit("Error: please enter at least 2 folder names containing the images with the original (comparator) folder first!")

#compare_colorspaces()