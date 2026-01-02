import axios from 'axios';

export function getRequest<T>(url: string): Promise<T | undefined> {
  return new Promise((resolve, reject) => {
    axios.get(url).then((response: any) => {
      resolve(response.data);
    }).
    catch((error: any) => {
      reject(error);
    });
  });
} 
