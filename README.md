# TaskAll

([Here you can find a detailed explanation](https://habr.com/ru/post/349352/))

 Using the library you can replace a code as follows:
 ```Cs
Task<int> get1 = Get1();
Task<int> get2 = Get2();
Task<string> get3Str = Get3Str();
Task<int> get4 = Get4();

await Task.WhenAll(get1, get2, get3Str, get4);

var result = get1.Result + get2.Result + int.Parse(get3Str.Result) + get4.Result;
 ```

 with
 ```Cs
 var result = await
    from val1  in Get1().AsParallel()
    from val2  in Get2().AsParallel()
    from val3S in Get3Str().AsParallel()
    from val4  in Get4().AsParallel()
    select val1 + val2 + int.Parse(val3S) + val4;
 ```

In case when an intermediate result is required you can use the library in the following way:

 ```Cs
var result = await
    from one in Get1().AsParallel()

    let oneA = one

    from two in Get2().AsParallel()
    from freeStr in Get3Str().AsParallel()

    let free = int.Parse(freeStr)//Intermediate expr 

    from eight in  Add5(free).AsSequential()//Here all the previous results can be used

    from oneB in Get1().AsParallel()
    from twoA in Get2().AsParallel()
    
    from six in Add5(oneB).AsSequential()//Here all the previous results can be used 
    
    select one + oneA + two + int.Parse(freeStr) + free + eight + oneB + twoA + six;
 ```
