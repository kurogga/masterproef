
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

ssim_array = []
psnr_array = []

def mse(x, y):
    return np.linalg.norm(x - y)

def get_ssim(x, y):
    return ssim(x, y, data_range=y.max() - y.min(), multichannel=True)

def get_psnr(x, y):
    _mse = mse(x, y)
    if _mse == 0:
        return 100
    PIXEL_MAX = 255.0
    return 20 * math.log10(PIXEL_MAX / math.sqrt(_mse))

def compare_images(x, y):
    original_file = cv2.imread(x)
    img = img_as_float(original_file)
    changed_file = cv2.imread(y)
    img_noise = img_as_float(changed_file)

#    ssim_none = get_ssim(img, img)
#    psnr_none = get_psnr(img,img)
    ssim_noise = get_ssim(img, img_noise)
    psnr_noise = get_psnr(img,img_noise)

#    ssim_array.append(ssim_none)
    ssim_array.append(ssim_noise)
#    psnr_array.append(psnr_none)
    psnr_array.append(psnr_noise)

def plot_graph():
    fig, axes = plt.subplots(nrows=1, ncols=2, figsize=(10, 4),
                         sharex=True, sharey=False)
    ax = axes.ravel()
    x_max = len(ssim_array)
    ax[0].plot(ssim_array)
    ax[0].set_ylabel('SSIM [%]')
    ax[0].set_xlabel('frame number')
    ax[0].set_title('Structural similarity')
    ax[0].set_ylim([0.0,1.0])
    ax[0].set_xlim([0,x_max+5])
    
    ax[1].plot(psnr_array)
    ax[1].set_ylabel('PSNR [dB]')
    ax[1].set_xlabel('frame number')
    ax[1].set_title('Peak signal-to-noise ratio')
    ax[1].set_ylim([10,60])
    
    plt.tight_layout()
    plt.show()
 
files = next(os.walk("original"))[2]
files2 = next(os.walk("changed"))[2]

if len(files)==0 and len(files)!=len(files2):
    sys.exit("Error: not enough files!")

for x in range(1,len(files)+1):    
    compare_images("original/"+str(x)+".png","changed/"+str(x)+".png")
    
plot_graph()

