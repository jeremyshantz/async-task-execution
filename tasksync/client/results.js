/** @jsx React.DOM */
'use strict';


var CallServer = function(url){

    return new Promise(function(resolve, reject){

        $.ajax({
            url: url,
            type: 'POST',
            dataType: 'json',
            success: function(data){
                resolve(data);
            },
            error: function(error){
                alert(JSON.stringify(error));
            }}
        );
    });
};


var Results = React.createClass({

    getInitialState: function () {
        return {
            results: [{ Value: 'testA' }, { Value: 'testB' }]
        };
    },

    getDefaultProps: function () {
        return {

        };
    },
    propTypes: {

    },

    mixins: {

    },

    statics: {

    },

    componentDidMount: function () {
        var pollcount = 0,
            pollmax = 80,
            pollinterval = 500;

        window.poop = this;

        var self = this;

        var GetResults;

        GetResults =  function (url) {

            CallServer(url).then(function(data){

                pollcount++;
                self.setState({ results: data});

            },function(error){
                alert(JSON.stringify(error));
            });
        };


        CallServer('http://localhost:63073/form/submitjson').then(function(data){

            var url = 'http://localhost:63073/form/resultsjson/' + data.ID;
            GetResults(url);

        },function(error){
            alert(JSON.stringify(error));
        });

    },
    render: function () {

        var items = this.state.results.map(function(result){
            return <LineItem result={result} />;

        });

        return (
            <ul class="results">
            {items}
            </ul>
            );
    }

});

var LineItem = React.createClass({

    render: function(){

        return (
            <li>{this.props.result.Value}</li>

            );

    }

});

React.renderComponent(<Results />, document.getElementById("results"));

